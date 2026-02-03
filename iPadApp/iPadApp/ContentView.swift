import SwiftUI

struct ContentView: View {
    @StateObject private var client = WebSocketClient()
    @StateObject private var audioStreamer = AudioStreamer()
    @StateObject private var cameraStreamer = CameraStreamer()
    private let outcomeClient = OutcomeClient()
    private let summaryStore = SummaryStore()
    @State private var host: String = "127.0.0.1:8000"
    @State private var isExpanded: Bool = false
    @State private var showOutcomeSheet: Bool = false
    @State private var isCoaching: Bool = false
    @State private var outcomeStatus: String?
    @State private var summaryText: String?
    @State private var shareURL: URL?
    @State private var showShareSheet: Bool = false

    var body: some View {
        NavigationStack {
            VStack(spacing: 16) {
                connectionCard
                coachingCard
                metricsCard
                sayNextCard
                summaryCard
                if let outcomeStatus {
                    Text(outcomeStatus)
                        .foregroundStyle(.secondary)
                }
                Spacer()
            }
            .padding()
            .navigationTitle("CoachPad")
        }
        .sheet(isPresented: $showOutcomeSheet) {
            outcomeSheet
        }
        .sheet(isPresented: $showShareSheet) {
            if let shareURL {
                ShareSheet(activityItems: [shareURL])
            }
        }
        .onAppear {
            summaryText = summaryStore.load()
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
            Toggle("Expanded dashboard", isOn: $isExpanded)
        }
        .padding()
        .background(.thinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var coachingCard: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Coaching")
                .font(.headline)
            HStack {
                Button(isCoaching ? "Stop Coaching" : "Start Coaching") {
                    if isCoaching {
                        stopCoaching()
                    } else {
                        startCoaching()
                    }
                }
                .buttonStyle(.borderedProminent)
                Button("End Call") {
                    showOutcomeSheet = true
                }
                .buttonStyle(.bordered)
            }
            HStack {
                Label("Mic", systemImage: audioStreamer.isRunning ? "mic.fill" : "mic.slash")
                Label("Camera", systemImage: cameraStreamer.isRunning ? "camera.fill" : "camera.slash")
            }
            if let error = audioStreamer.lastError {
                Text(error)
                    .foregroundStyle(.red)
            }
            if let error = cameraStreamer.lastError {
                Text(error)
                    .foregroundStyle(.red)
            }
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
                        if isExpanded {
                            GridRow {
                                Text("Last update")
                                Text("\(metrics.lastUpdateMs)")
                            }
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

    private var summaryCard: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Call summary")
                .font(.headline)
            if let summaryText {
                Text(summaryText)
                    .font(.footnote)
                Button("Export PDF") {
                    if let url = summaryStore.exportPDF(summary: summaryText) {
                        shareURL = url
                        showShareSheet = true
                    }
                }
                .buttonStyle(.bordered)
            } else {
                Text("No summary yet")
                    .foregroundStyle(.secondary)
            }
        }
        .padding()
        .background(.thinMaterial)
        .clipShape(RoundedRectangle(cornerRadius: 12))
    }

    private var outcomeSheet: some View {
        NavigationStack {
            List {
                Button("Meeting booked") { submitOutcome("meeting_booked") }
                Button("Follow up") { submitOutcome("follow_up") }
                Button("Lost") { submitOutcome("lost") }
            }
            .navigationTitle("Call outcome")
        }
    }

    private func startCoaching() {
        isCoaching = true
        audioStreamer.start(host: host)
        cameraStreamer.start(host: host)
        client.connect(to: host)
    }

    private func stopCoaching() {
        isCoaching = false
        audioStreamer.stop()
        cameraStreamer.stop()
        client.disconnect()
    }

    private func submitOutcome(_ outcome: String) {
        Task {
            do {
                try await outcomeClient.sendOutcome(host: host, outcome: outcome)
                let summary = try await outcomeClient.fetchSummary(host: host)
                if !summary.isEmpty {
                    summaryText = summary
                    _ = try? summaryStore.save(summary: summary)
                }
                outcomeStatus = "Outcome sent"
                showOutcomeSheet = false
            } catch {
                outcomeStatus = "Failed to send outcome"
            }
        }
    }
}

#Preview {
    ContentView()
}
