import Foundation

struct LiveMetrics: Codable {
    let talkListenRatio: Double
    let questionsPerMinute: Double
    let sentiment: Double
    let engagement: Double
    let methodologyStage: String
    let sayNext: [String]
    let lastUpdateMs: Int

    enum CodingKeys: String, CodingKey {
        case talkListenRatio = "talk_listen_ratio"
        case questionsPerMinute = "questions_per_minute"
        case sentiment
        case engagement
        case methodologyStage = "methodology_stage"
        case sayNext = "say_next"
        case lastUpdateMs = "last_update_ms"
    }
}

enum ConnectionState: String {
    case disconnected
    case connecting
    case connected
    case error
}
