import Foundation
import AVFoundation
import UIKit

final class CameraStreamer: NSObject, ObservableObject, AVCaptureVideoDataOutputSampleBufferDelegate {
    @Published var isRunning: Bool = false
    @Published var lastError: String?

    private let session = AVCaptureSession()
    private let output = AVCaptureVideoDataOutput()
    private var wsTask: URLSessionWebSocketTask?
    private let queue = DispatchQueue(label: "camera.stream.queue")
    private var lastSentAt: TimeInterval = 0
    private let fpsLimit: Double = 3.0

    func start(host: String) {
        stop()
        guard let url = URL(string: "ws://\(host)/ws/vision") else {
            lastError = "Invalid host"
            return
        }
        let sessionConfig = URLSession(configuration: .default)
        wsTask = sessionConfig.webSocketTask(with: url)
        wsTask?.resume()

        session.beginConfiguration()
        session.sessionPreset = .vga640x480

        guard let device = AVCaptureDevice.default(.builtInWideAngleCamera, for: .video, position: .front) else {
            lastError = "No front camera"
            return
        }
        do {
            let input = try AVCaptureDeviceInput(device: device)
            if session.canAddInput(input) {
                session.addInput(input)
            }
        } catch {
            lastError = error.localizedDescription
            return
        }

        output.videoSettings = [kCVPixelBufferPixelFormatTypeKey as String: kCVPixelFormatType_32BGRA]
        output.setSampleBufferDelegate(self, queue: queue)
        output.alwaysDiscardsLateVideoFrames = true
        if session.canAddOutput(output) {
            session.addOutput(output)
        }

        session.commitConfiguration()
        session.startRunning()
        isRunning = true
    }

    func stop() {
        session.stopRunning()
        wsTask?.cancel(with: .goingAway, reason: nil)
        wsTask = nil
        isRunning = false
    }

    func captureOutput(_ output: AVCaptureOutput, didOutput sampleBuffer: CMSampleBuffer, from connection: AVCaptureConnection) {
        let now = Date().timeIntervalSince1970
        if now - lastSentAt < 1.0 / fpsLimit {
            return
        }
        lastSentAt = now
        guard let imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer) else { return }
        let ciImage = CIImage(cvImageBuffer: imageBuffer)
        let context = CIContext()
        guard let cgImage = context.createCGImage(ciImage, from: ciImage.extent) else { return }
        let uiImage = UIImage(cgImage: cgImage)
        guard let data = uiImage.jpegData(compressionQuality: 0.6) else { return }
        wsTask?.send(.data(data)) { [weak self] error in
            if let error {
                DispatchQueue.main.async {
                    self?.lastError = error.localizedDescription
                }
            }
        }
    }
}
