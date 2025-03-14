import ActivityKit
import Foundation
import WidgetKit

public struct GameActivityAttributes: ActivityAttributes {
    public init() {}
    public init(from decoder: any Decoder) throws {
    }
    public struct ContentState: Codable & Hashable {
        public var servers: [String] = []
        public var messages: UInt32 = 0
        public var players: UInt32 = 0
        public var maxPlayers: UInt32 = 32
        public init() {}
        public init(servers: [String], messages: UInt32, players: UInt32 = 0, maxPlayers: UInt32 = 32) {
            self.servers = servers
            self.messages = messages
            self.players = players
            self.maxPlayers = maxPlayers
        }
        public init(from decoder: any Decoder) throws {
            let container: KeyedDecodingContainer<GameActivityAttributes.ContentState.CodingKeys> = try decoder.container(keyedBy: GameActivityAttributes.ContentState.CodingKeys.self)
            self.servers = try container.decode([String].self, forKey: GameActivityAttributes.ContentState.CodingKeys.servers)
            self.messages = try container.decode(UInt32.self, forKey: GameActivityAttributes.ContentState.CodingKeys.messages)
            self.players = try container.decode(UInt32.self, forKey: GameActivityAttributes.ContentState.CodingKeys.players)
            self.maxPlayers = try container.decode(UInt32.self, forKey: GameActivityAttributes.ContentState.CodingKeys.maxPlayers)
        }
        public static func ==(left: ContentState, right: ContentState) -> Bool {
            if left.servers.count != right.servers.count
                || (left.servers.count == 1 && left.servers[0] != right.servers[0])
                || left.messages != right.messages
                || left.players != right.players
                || left.maxPlayers != right.maxPlayers {
                return false
            } else {
                return true
            }
        }
    }
}

@objc(LiveActivityShared) public final class LiveActivityShared : NSObject {
    static var disconnectHandler: () -> Void = {}
    @objc public static func refreshWidgets(ofKind: String? = nil) {
        if let kind = ofKind {
            WidgetCenter.shared.reloadTimelines(ofKind: kind)
        } else {
            WidgetCenter.shared.reloadAllTimelines()
        }
    }
    @objc public static func showLiveActivity(servers: [String], messages: UInt32, players: UInt32 = 0, maxPlayers: UInt32 = 32, disconnectHandler: @escaping () -> Void) async {
        if ActivityAuthorizationInfo().areActivitiesEnabled {
            let state = GameActivityAttributes.ContentState(servers: servers, messages: messages, players: players, maxPlayers: maxPlayers)
            LiveActivityShared.disconnectHandler = disconnectHandler
            let activities = Activity<GameActivityAttributes>.activities
            if activities.count > 0 {
                let activity = activities.first!
/*                let alertConfiguration = AlertConfiguration(title: "You are connected to \(servers.count) server \(servers.count > 1 ? "s" : "")",
                                                            body: "You have \(messages) unread message\((messages > 1 ? "s" : ""))",
                                                            sound: .default)*/
                let oldState = activity.content.state
                if oldState == state {
                    return
                }
                await activity.update(ActivityContent<Activity<GameActivityAttributes>.ContentState>(
                    state: state, staleDate: nil
                ), alertConfiguration: nil)
            } else {
                do {
                    let attributes = GameActivityAttributes()
                    
                    _ = try Activity.request(
                        attributes: attributes,
                        content: .init(state: state, staleDate: nil),
                        pushType: nil
                    )
                    
                } catch {
                    let errorMessage = """
                        Couldn't start activity
                        ------------------------
                        \(String(describing: error))
                        """
                    
                    print(errorMessage)
                }
            }
        }
    }
    @objc public static func stopLiveActivity() async {
        if ActivityAuthorizationInfo().areActivitiesEnabled {
            let state = GameActivityAttributes.ContentState(servers: [], messages: 0)
            let activities = Activity<GameActivityAttributes>.activities
            if activities.count > 0 {
                let activity = activities.first!
                let dismissalPolicy = ActivityUIDismissalPolicy.after(.now)
                await activity.end(ActivityContent(state: state, staleDate: nil), dismissalPolicy: dismissalPolicy)
            }
        }
    }
    public static func disconnect() {
        LiveActivityShared.disconnectHandler()
    }
}

