// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "CoachPadPlayground",
    platforms: [
        .iOS(.v17)
    ],
    products: [
        .executable(name: "CoachPadPlayground", targets: ["CoachPadPlayground"])
    ],
    targets: [
        .executableTarget(
            name: "CoachPadPlayground",
            path: "Sources/CoachPadPlayground"
        )
    ]
)
