import Foundation

final class OutcomeClient {
    func sendOutcome(host: String, outcome: String) async throws {
        guard let url = URL(string: "http://\(host)/outcome") else {
            throw URLError(.badURL)
        }
        var request = URLRequest(url: url)
        request.httpMethod = "POST"
        request.setValue("application/json", forHTTPHeaderField: "Content-Type")
        let payload = ["outcome": outcome]
        request.httpBody = try JSONSerialization.data(withJSONObject: payload, options: [])
        let (_, response) = try await URLSession.shared.data(for: request)
        guard let http = response as? HTTPURLResponse, http.statusCode == 200 else {
            throw URLError(.badServerResponse)
        }
    }

    func fetchSummary(host: String) async throws -> String {
        guard let url = URL(string: "http://\(host)/summary") else {
            throw URLError(.badURL)
        }
        let (data, response) = try await URLSession.shared.data(from: url)
        guard let http = response as? HTTPURLResponse, http.statusCode == 200 else {
            throw URLError(.badServerResponse)
        }
        let payload = try JSONSerialization.jsonObject(with: data, options: []) as? [String: Any]
        return payload?["summary"] as? String ?? ""
    }
}
