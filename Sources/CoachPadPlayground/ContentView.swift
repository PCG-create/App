import SwiftUI

struct ContentView: View {
    @StateObject private var client = WebSocketClient()
    @StateObject private var sender = TranscriptSender()
    @State private var host: String = "127.0.0.1:8000"

    var body: some View {
        NavigationStack {
            VStack(spacing: 16) {
                connectionCard
                metricsCard
                sayNextCard
                if let error = sender.lastError {
                    Text(error)
                        .foregroundStyle(.red)
                }
                Spacer()
            }
            .padding()
            .navigationTitle("CoachPad Playground")
        }
    }

    private var connectionCard: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Connection")
                .font(.headline)
            HStack {
                TextField("Host", text: $host)
                    .textFieldStyle(.roundedBorder)
                Button("Connect") {
                    client.connect(to: host)
                }
                .buttonStyle(.borderedProminent)
                Button("Disconnect") {
                    client.disconnect()
                }
                .buttonStyle(.bordered)
            }
            HStack {
                Text("State: \(client.connectionState.rawValue)")
                if let error = client.lastError {
                    Text(error)
                        .foregroundStyle(.red)
                }
            }
            Button("Send sample transcript") {
                sender.sendSample(host: host)
            }
            .buttonStyle(.bordered)
        }
        .padding()
        .background(.thinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var metricsCard: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Live Metrics")
                .font(.headline)
            if let metrics = client.latestMetrics {
                Grid(alignment: .leading, horizontalSpacing: 12, verticalSpacing: 8) {
                    GridRow {
                        Text("Talk to listen")
                        Text(String(format: "%.2f", metrics.talkListenRatio))
                    }
                    GridRow {
                        Text("Questions per minute")
                        Text(String(format: "%.2f", metrics.questionsPerMinute))
                    }
                    GridRow {
                        Text("Sentiment")
                        Text(String(format: "%.2f", metrics.sentiment))
                    }
                    GridRow {
                        Text("Engagement")
                        Text(String(format: "%.2f", metrics.engagement))
                    }
                    GridRow {
                        Text("Stage")
                        Text(metrics.methodologyStage)
                    }
                }
            } else {
                Text("Waiting for metrics")
                    .foregroundStyle(.secondary)
            }
        }
        .padding()
        .background(.thinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var sayNextCard: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Say this next")
                .font(.headline)
            if let metrics = client.latestMetrics, !metrics.sayNext.isEmpty {
                ForEach(metrics.sayNext, id: \.self) { line in
                    Text("• \(line)")
                        .font(.body)
                }
            } else {
                Text("No coaching lines yet")
                    .foregroundStyle(.secondary)
            }
        }
        .padding()
        .background(.thinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }
}
