import Foundation

final class TranscriptSender: ObservableObject {
    @Published var lastError: String?

    func sendSample(host: String) {
        guard let url = URL(string: "ws://\(host)/ws/ingest") else {
            lastError = "Invalid host"
            return
        }
        let session = URLSession(configuration: .default)
        let task = session.webSocketTask(with: url)
        task.resume()

        let samples: [(String, String)] = [
            ("rep", "Thanks for taking the time today. How are things going?"),
            ("prospect", "Busy, we have a lot of manual steps in our process."),
            ("rep", "What does your current workflow look like?"),
            ("prospect", "We use spreadsheets and it slows us down."),
            ("rep", "How is that impacting your team right now?")
        ]

        Task {
            for item in samples {
                let payload: [String: Any] = [
                    "speaker": item.0,
                    "text": item.1,
                    "timestamp_ms": Int(Date().timeIntervalSince1970 * 1000)
                ]
                do {
                    let data = try JSONSerialization.data(withJSONObject: payload, options: [])
                    let text = String(data: data, encoding: .utf8) ?? ""
                    try await task.send(.string(text))
                    _ = try await task.receive()
                    try await Task.sleep(nanoseconds: 800_000_000)
                } catch {
                    await MainActor.run {
                        self.lastError = error.localizedDescription
                    }
                    break
                }
            }
            task.cancel(with: .goingAway, reason: nil)
        }
    }
}
