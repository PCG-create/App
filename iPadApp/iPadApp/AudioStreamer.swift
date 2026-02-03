import Foundation
import AVFoundation

final class AudioStreamer: NSObject, ObservableObject {
    @Published var isRunning: Bool = false
    @Published var lastError: String?

    private let engine = AVAudioEngine()
    private var wsTask: URLSessionWebSocketTask?
    private let queue = DispatchQueue(label: "audio.stream.queue")
    private var converter: AVAudioConverter?

    func start(host: String) {
        stop()
        guard let url = URL(string: "ws://\(host)/ws/audio") else {
            lastError = "Invalid host"
            return
        }
        let session = URLSession(configuration: .default)
        wsTask = session.webSocketTask(with: url)
        wsTask?.resume()

        let input = engine.inputNode
        let inputFormat = input.outputFormat(forBus: 0)
        let outputFormat = AVAudioFormat(commonFormat: .pcmFormatInt16, sampleRate: 16000, channels: 1, interleaved: true)
        guard let outputFormat else {
            lastError = "Failed to create audio format"
            return
        }
        converter = AVAudioConverter(from: inputFormat, to: outputFormat)

        let bufferSize: AVAudioFrameCount = 1024
        input.installTap(onBus: 0, bufferSize: bufferSize, format: inputFormat) { [weak self] buffer, _ in
            self?.process(buffer: buffer, outputFormat: outputFormat)
        }

        do {
            try AVAudioSession.sharedInstance().setCategory(.playAndRecord, options: [.defaultToSpeaker, .allowBluetooth])
            try AVAudioSession.sharedInstance().setActive(true)
            try engine.start()
            isRunning = true
        } catch {
            lastError = error.localizedDescription
            stop()
        }
    }

    func stop() {
        engine.inputNode.removeTap(onBus: 0)
        engine.stop()
        wsTask?.cancel(with: .goingAway, reason: nil)
        wsTask = nil
        isRunning = false
    }

    private func process(buffer: AVAudioPCMBuffer, outputFormat: AVAudioFormat) {
        guard let converter else { return }
        let outputBuffer = AVAudioPCMBuffer(pcmFormat: outputFormat, frameCapacity: 1600)
        guard let outputBuffer else { return }
        var error: NSError?
        let inputBlock: AVAudioConverterInputBlock = { _, outStatus in
            outStatus.pointee = .haveData
            return buffer
        }
        converter.convert(to: outputBuffer, error: &error, withInputFrom: inputBlock)
        if let error {
            DispatchQueue.main.async {
                self.lastError = error.localizedDescription
            }
            return
        }
        guard let channelData = outputBuffer.int16ChannelData else { return }
        let frames = Int(outputBuffer.frameLength)
        let data = Data(bytes: channelData[0], count: frames * MemoryLayout<Int16>.size)
        queue.async { [weak self] in
            self?.wsTask?.send(.data(data)) { sendError in
                if let sendError {
                    DispatchQueue.main.async {
                        self?.lastError = sendError.localizedDescription
                    }
                }
            }
        }
    }
}
