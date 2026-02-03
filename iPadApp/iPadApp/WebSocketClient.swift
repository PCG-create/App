import Foundation
import Combine
import UIKit

final class WebSocketClient: ObservableObject {
    @Published var connectionState: ConnectionState = .disconnected
    @Published var latestMetrics: LiveMetrics?
    @Published var lastError: String?

    private var task: URLSessionWebSocketTask?
    private var pingTimer: Timer?
    private let haptic = UINotificationFeedbackGenerator()
    private var lastHapticAt: Date = .distantPast

    func connect(to host: String) {
        disconnect()
        guard let url = URL(string: "ws://\(host)/ws/ui") else {
            lastError = "Invalid host"
            connectionState = .error
            return
        }
        connectionState = .connecting
        let session = URLSession(configuration: .default)
        task = session.webSocketTask(with: url)
        task?.resume()
        receiveLoop()
        startPing()
        connectionState = .connected
    }

    func disconnect() {
        pingTimer?.invalidate()
        pingTimer = nil
        task?.cancel(with: .goingAway, reason: nil)
        task = nil
        connectionState = .disconnected
    }

    private func startPing() {
        pingTimer?.invalidate()
        pingTimer = Timer.scheduledTimer(withTimeInterval: 10, repeats: true) { [weak self] _ in
            self?.task?.send(.string("ping")) { error in
                if let error {
                    DispatchQueue.main.async {
                        self?.lastError = error.localizedDescription
                        self?.connectionState = .error
                    }
                }
            }
        }
    }

    private func receiveLoop() {
        task?.receive { [weak self] result in
            guard let self else { return }
            switch result {
            case .failure(let error):
                DispatchQueue.main.async {
                    self.lastError = error.localizedDescription
                    self.connectionState = .error
                }
            case .success(let message):
                switch message {
                case .string(let text):
                    self.handle(text: text)
                case .data(let data):
                    if let text = String(data: data, encoding: .utf8) {
                        self.handle(text: text)
                    }
                @unknown default:
                    break
                }
            }
            self.receiveLoop()
        }
    }

    private func handle(text: String) {
        guard let data = text.data(using: .utf8) else { return }
        if let metrics = try? JSONDecoder().decode(LiveMetrics.self, from: data) {
            DispatchQueue.main.async {
                self.latestMetrics = metrics
                self.connectionState = .connected
                self.maybeHaptic(metrics: metrics)
            }
        }
    }

    private func maybeHaptic(metrics: LiveMetrics) {
        let now = Date()
        if now.timeIntervalSince(lastHapticAt) < 5 {
            return
        }
        if metrics.talkListenRatio > 2.0 || metrics.engagement < 0.4 || metrics.sentiment < -0.2 {
            haptic.notificationOccurred(.warning)
            lastHapticAt = now
        }
    }
}
