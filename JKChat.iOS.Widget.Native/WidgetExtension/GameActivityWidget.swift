import WidgetKit
import AppIntents
import SwiftUI
import ActivityKit
import WidgetShared

@available(iOS 17.0, *)
struct GameActivityWidget: Widget {
    var body: some WidgetConfiguration {
        ActivityConfiguration(for: GameActivityAttributes.self) { context in
            // Create the presentation that appears on the Lock Screen and as a
            // banner on the Home Screen of devices that don't support the
            // Dynamic Island.
            GameActivityView(state: context.state)
                .widgetURL(URL(string: "jkchat://liveactivity"))
        } dynamicIsland: { context in
            // Create the presentations that appear in the Dynamic Island.
            DynamicIsland {
                // Create the expanded presentation.
                expandedContent(state: context.state)
            } compactLeading: {
                // Create the compact leading presentation.
                GameActivityCompactLeadingView(state: context.state)
            } compactTrailing: {
                // Create the compact trailing presentation.
                GameActivityCompactTrailingView(state: context.state)
            } minimal: {
                // Create the minimal presentation.
                GameActivityMinimalView(state: context.state)
            }
            .widgetURL(URL(string: "jkchat://liveactivity"))
        }
    }
    
    @DynamicIslandExpandedContentBuilder
    private func expandedContent(state: GameActivityAttributes.ContentState) -> DynamicIslandExpandedContent<some View> {
        DynamicIslandExpandedRegion(.leading, priority: 3.0) {
            GameActivityLeadingView(state: state)
            .frame(maxWidth: .infinity)
            .fixedSize(horizontal: false, vertical: true)
//            .background(.blue)
            .dynamicIsland(verticalPlacement: .belowIfTooWide)
        }
        
        DynamicIslandExpandedRegion(.bottom, priority: 1.0) {
            GameActivityBottomView(state: state)
        }
    }
}

func getTitle(servers: [String]) -> AttributedString {
    if servers.count > 1 {
        AttributedString("Connected to \(servers.count) servers")
    } else {
        convertToColourText(value: "Connected to \(servers[0])", systemColors: true)
    }
}

func getMessages(messages: UInt32) -> String {
    "\(messages) unread message\(messages > 1 ? "s" : "")"
}

@available(iOS 17.0, *)
struct GameActivityView : View {
    let state: GameActivityAttributes.ContentState
    var body: some View {
        GameActivityLeadingView(state: state)
        .padding(16.0)
    }
}

@available(iOS 17.0, *)
struct GameActivityLeadingView : View {
    let state: GameActivityAttributes.ContentState
    var body: some View {
        VStack(alignment: .leading, spacing: 0.0) {
            let lineLimit = state.servers.count == 1 && state.messages > 0 ? 1 : 2
            HStack(alignment: .center, spacing: 4.0) {
                Image(systemName: "network")
                    .font(.headline)
                Text(getTitle(servers: state.servers))
                    .font(.headline)
                    .multilineTextAlignment(.leading)
                    .lineLimit(lineLimit, reservesSpace: false)
            }
            HStack(alignment: .center, spacing: 4.0) {
                if state.servers.count == 1 {
                    Image(systemName: "person")
                        .font(.subheadline)
                    Text("\(state.players)/\(state.maxPlayers) players")
                        .font(.subheadline)
                        .multilineTextAlignment(.leading)
                }
            }
            HStack(alignment: .center, spacing: 4.0) {
                if state.messages > 0 {
                    Image(systemName: "message.badge")
                        .font(.subheadline)
                    Text(getMessages(messages: state.messages))
                        .font(.subheadline)
                        .multilineTextAlignment(.leading)
                }
            }
            Spacer(minLength: 0.0)
                .frame(maxWidth: .infinity)
        }
    }
}

@available(iOS 17.0, *)
struct GameActivityBottomView : View {
    let state: GameActivityAttributes.ContentState
    var body: some View {
        if state.servers.count > 0 {
            VStack() {
                Spacer()
                Button("Disconnect\(state.servers.count > 1 ? " from all" : "")", intent: DisconnectIntent())
                    .tint(.mint)
            }
        }
    }
}

@available(iOS 17.0, *)
public struct DisconnectIntent: AppIntent {
    public static var title: LocalizedStringResource = "Disconnect"
    
    public init() {}
    
    public func perform() async throws -> some IntentResult {
        guard let userDefaults = UserDefaults(suiteName: "group.com.vlbor.JKChat") else {
            return .result()
        }
        
        userDefaults.set(true, forKey: "LiveActivityDisconnect")
        
//        LiveActivityShared.disconnect()
        return .result()
    }
}

@available(iOS 17.0, *)
struct GameActivityCompactLeadingView : View {
    let state: GameActivityAttributes.ContentState
    var body: some View {
        HStack(alignment: .center, spacing: 0.0) {
            Image(systemName: "network")
                .padding(.leading, 5.0)
            Text(String(state.servers.count))
        }
    }
}

@available(iOS 17.0, *)
struct GameActivityCompactTrailingView : View {
    let state: GameActivityAttributes.ContentState
    var body: some View {
        HStack(alignment: .center, spacing: 0.0) {
            if state.messages == 0 && state.servers.count == 1 {
                Image(systemName: "person")
                Text("\(state.players)/\(state.maxPlayers)")
            } else {
                Image(systemName: "message.badge")
                Text(String(state.messages))
            }
        }
    }
}

@available(iOS 17.0, *)
struct GameActivityMinimalView : View {
    let state: GameActivityAttributes.ContentState
    var body: some View {
        if state.messages > 0 {
            let fontSize = switch state.messages {
                case 100...: Font.system(size: 10.0)
                case 10...: Font.system(size: 12.0)
                default: Font.body
            }
            HStack(alignment: .center, spacing: 0.0) {
                Image(systemName: "message.badge")
                    .font(fontSize)
                Text(String(state.messages))
                    .minimumScaleFactor(0.2)
            }
        } else {
            HStack(alignment: .center, spacing: 0.0) {
                Image(systemName: "network")
                Text(String(state.servers.count))
                    .minimumScaleFactor(0.7)
            }
        }
    }
}

@available(iOS 17.0, *)
struct LiveActivitiesPreviewProvider: PreviewProvider {
    static let activityAttributes = GameActivityAttributes()
    
    static let state = GameActivityAttributes.ContentState(servers: ["^0Re^3fresh-01 ^0Re^7fresh-01 ^0Re^3fresh-01 ^0Re^3fresh-01 ^0Re^3fresh-01 ^0Re^3fresh-01 ^0Re^3fresh-01"/*, "^1Re^5fresh-02"*/], messages: 5, players: 32, maxPlayers: 64)
    
    static var previews: some View {
        activityAttributes
            .previewContext(state, viewKind: .dynamicIsland(.compact))
            .previewDisplayName("Compact")
        
        activityAttributes
            .previewContext(state, viewKind: .dynamicIsland(.expanded))
            .previewDisplayName("Expanded")
        
        activityAttributes
            .previewContext(state, viewKind: .content)
            .previewDisplayName("Notification")
        
        activityAttributes
            .previewContext(state, viewKind: .dynamicIsland(.minimal))
            .previewDisplayName("Minimal")

    }
}
