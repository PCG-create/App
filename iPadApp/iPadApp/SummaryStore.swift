import Foundation
import PDFKit
import UIKit

final class SummaryStore {
    private let fileName = "last_summary.txt"

    func save(summary: String) throws -> URL {
        let url = try summaryURL()
        let data = summary.data(using: .utf8) ?? Data()
        try data.write(to: url, options: .completeFileProtection)
        return url
    }

    func load() -> String? {
        guard let url = try? summaryURL() else { return nil }
        return try? String(contentsOf: url)
    }

    func exportPDF(summary: String) -> URL? {
        let pdfDoc = PDFDocument()
        let page = PDFPage(image: renderText(summary))
        if let page { pdfDoc.insert(page, at: 0) }
        guard let data = pdfDoc.dataRepresentation() else { return nil }
        guard let url = try? summaryPDFURL() else { return nil }
        try? data.write(to: url, options: .completeFileProtection)
        return url
    }

    private func renderText(_ text: String) -> UIImage {
        let size = CGSize(width: 612, height: 792)
        let renderer = UIGraphicsImageRenderer(size: size)
        return renderer.image { ctx in
            UIColor.white.setFill()
            ctx.fill(CGRect(origin: .zero, size: size))
            let paragraphStyle = NSMutableParagraphStyle()
            paragraphStyle.lineBreakMode = .byWordWrapping
            let attrs: [NSAttributedString.Key: Any] = [
                .font: UIFont.systemFont(ofSize: 14),
                .paragraphStyle: paragraphStyle
            ]
            let rect = CGRect(x: 24, y: 24, width: size.width - 48, height: size.height - 48)
            text.draw(in: rect, withAttributes: attrs)
        }
    }

    private func summaryURL() throws -> URL {
        let dir = try FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true)
        return dir.appendingPathComponent(fileName)
    }

    private func summaryPDFURL() throws -> URL {
        let dir = try FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true)
        return dir.appendingPathComponent("last_summary.pdf")
    }
}
