import WidgetKit
import SwiftUI
import AppIntents
import Network

@available(iOS 17.0, *)
struct Provider: AppIntentTimelineProvider {
    func placeholder(in context: Context) -> ServerMonitorEntry {
        ServerMonitorEntry(date: Date(), configuration: ConfigurationAppIntent(), family: context.family)
    }

    func snapshot(for configuration: ConfigurationAppIntent, in context: Context) async -> ServerMonitorEntry {
        ServerMonitorEntry(date: Date(), configuration: configuration, family: context.family, isPlaceholder: true)
    }
    
    func timeline(for configuration: ConfigurationAppIntent, in context: Context) async -> Timeline<ServerMonitorEntry> {
        var entries: [ServerMonitorEntry] = []
        do {
            if var entry = try await loadServerInfo(configuration: configuration) {
                entry.family = context.family
                entries.append(entry)
            } else {
                entries.append(ServerMonitorEntry(date: .now, configuration: configuration, family: context.family))
            }
        } catch {
            print(error)
        }

        return Timeline(entries: entries, policy: .atEnd)
    }
}

@available(iOS 17.0, *)
func loadServerInfo(configuration: ConfigurationAppIntent) async throws -> ServerMonitorEntry? {
    let entity = configuration.server
    if entity.isEmpty {
        return ServerMonitorEntry(date: .now, configuration: configuration, isEmpty: true)
    }
    return try await withCheckedThrowingContinuation({ (continuation: CheckedContinuation<ServerMonitorEntry?, Error>) in
        let hostUDP = NWEndpoint.Host(entity.address)
        let portUDP = NWEndpoint.Port(rawValue: entity.port)!
        let connection = NWConnection(host: hostUDP, port: portUDP, using: .udp)
        connection.stateUpdateHandler = { (newState) in
            print("This is stateUpdateHandler:")
            switch (newState) {
                case .ready:
                    print("State: Ready\n")
//                sendUDP(connection: connection, content: "getinfo xxx")
//                receiveUDP(connection: connection)
                Task {
                    let result = try await sendUDP(connection: connection, content: "getstatus")
                    
                    if !result {
                        continuation.resume(returning: nil)
                    }
                
                    let response = try await receiveUDP(connection: connection)
                    let components = response!.components(separatedBy: "\n")
                    if (components[0].contains("statusResponse")) {
                        let firstIndex = components[1].firstIndex(of: "\\")
                        let index = components[1].distance(from: components[1].startIndex, to: firstIndex!)
                        let infoString = components[1].components(separatedBy: "\\")
                        let start = index != 0 ? 0 : 1
                        let length = (infoString.count-start) & ~1
                        var infoDictionary: [String : String] = [:]
                        for i in stride(from: start, to: length, by: 2) {
                            infoDictionary[infoString[i]] = infoString[i+1]
                        }
                        var serverName: String = "Server Name"
                        if let val = infoDictionary["sv_hostname"] {
                            serverName = val
                        }
                        var maxPlayers: Int = 32
                        if let val = infoDictionary["sv_maxclients"] {
                            maxPlayers = Int(val) ?? 32
                        }
                        var mapName: String = "server_map"
                        if let val = infoDictionary["mapname"] {
                            mapName = val
                        }
                        var players: [ServerMonitorPlayer] = []
                        for i in 2..<components.count {
                            let playerInfo = components[i].components(separatedBy: " ")
                            if playerInfo.count > 2 {
                                let playerPing = Int(playerInfo[1]) ?? 0
                                if playerPing > 0 {
                                    var playerName: String
                                    if playerInfo.count > 3 {
                                        playerName = playerInfo[2..<playerInfo.count].joined(separator: " ")
                                    } else {
                                        playerName = playerInfo[2]
                                    }
                                    playerName = playerName.replacingOccurrences(of: "\"", with: "")
                                    let playerScore = playerInfo[0]
                                    players.append(ServerMonitorPlayer(name: playerName))
                                }
                            }
                        }
                        continuation.resume(returning: ServerMonitorEntry(date: .now, configuration: configuration, address: entity.id , serverName: serverName, players: players, maxPlayers: maxPlayers, mapName: mapName))
                    }
                }
                case .setup:
                    print("State: Setup\n")
                case .cancelled:
                    print("State: Cancelled\n")
                    continuation.resume(returning: nil)
                case .preparing:
                    print("State: Preparing\n")
                default:
                    print("ERROR! State not defined!\n")
//                    continuation.resume(returning: nil)
            }
        }
        connection.start(queue: .global())
    })
}

func sendUDP(connection: NWConnection, content: String) async throws -> Bool {
    return try await withCheckedThrowingContinuation({ (continuation: CheckedContinuation<Bool, Error>) in
        let data = content.data(using: .ascii)!
        let sendData = NSMutableData(bytes: [0xFFFFFFFF], length: 4)
        sendData.append(data)
        print(sendData)
        connection.send(content: sendData, completion: NWConnection.SendCompletion.contentProcessed(({ (NWError) in
            if (NWError == nil) {
                print("Data was sent to UDP")
                continuation.resume(returning: true)
            } else {
                print("ERROR! Error when data (Type: Data) sending. NWError: \n \(NWError!)")
                continuation.resume(returning: false)
            }
        })))
    })
}

func receiveUDP(connection: NWConnection) async throws -> String? {
     return try await withCheckedThrowingContinuation({ (continuation: CheckedContinuation<String?, Error>) in
         connection.receiveMessage { (data, context, isComplete, error) in
             if (isComplete) {
                 print("Receive is complete")
                 if (data != nil) {
                     let response = String(data: data!, encoding: String.Encoding.ascii)
                     continuation.resume(returning: response)
                 } else {
                     continuation.resume(returning: nil)
                 }
             }
         }
     })
}

func convertToColourText(value: String) -> AttributedString {
    if value.count <= 0 {
        return AttributedString()
    }
    var cleanValue = ""
    var colorLength = 0;
    var colorCodes: [Int] = []
    var colorStarts: [Int] = []
    var colorLengths: [Int] = []
    let characters = Array(value)
    var i = 0
    while (i < characters.count) {
        if characters[i] == "^" && ((i+1) < characters.count) && characters[i+1].isWholeNumber {
            colorLength = 0
            colorCodes.append(characters[i+1].wholeNumberValue ?? 0)
            colorStarts.append(cleanValue.count)
            colorLengths.append(0)
            i += 2
            continue
        }
        if (colorLengths.count > 0) {
            colorLength += 1
            colorLengths[colorLengths.count-1] = colorLength;
        }
        cleanValue.append(characters[i])
        i += 1
    }
    let attributedString = NSMutableAttributedString(string: cleanValue)
    for i in 0..<colorCodes.count {
        attributedString.addAttribute(.foregroundColor, value: UIColor(gameColor(code: colorCodes[i])), range: NSRange(location: colorStarts[i], length: colorLengths[i]))
    }
    return AttributedString(attributedString)
}

func gameColor(code: Int) -> Color {
    return switch code {
    case 0, 8: Color(red: 0, green: 0, blue: 0)
    case 1, 9: Color(red: 1, green: 0, blue: 0)
    case 2: Color(red: 0, green: 1, blue: 0)
    case 3: Color(red: 1, green: 1, blue: 0)
    case 4: Color(red: 0, green: 0, blue: 1)
    case 5: Color(red: 0, green: 1, blue: 1)
    case 6: Color(red: 1, green: 0, blue: 1)
    case 7: Color(red: 1, green: 1, blue: 1)
    default: Color(red: 1, green: 1, blue: 1)
    }
}

@available(iOS 17.0, *)
struct ServerMonitorEntry: TimelineEntry {
    let date: Date
    let configuration: ConfigurationAppIntent
    var address: String = ""
    var serverName: String = "Server Name"
    var players: [ServerMonitorPlayer] = [ServerMonitorPlayer(name: "Player 1"), ServerMonitorPlayer(name: "Player 2"), ServerMonitorPlayer(name: "Player 3"), ServerMonitorPlayer(name: "Player 4"), ServerMonitorPlayer(name: "Player 5"), ServerMonitorPlayer(name: "Player 6"), ServerMonitorPlayer(name: "Player 7"), ServerMonitorPlayer(name: "Player 8")]
    var maxPlayers: Int = 32
    var mapName: String = "server_map"
    var family: WidgetFamily = .systemMedium
    var isEmpty: Bool = false
    var isPlaceholder: Bool = false
}

struct ServerMonitorPlayer: Identifiable {
    let name: String
    let id = UUID()
}

@available(iOS 17.0, *)
struct WidgetEntryView : View {
    let entry: ServerMonitorEntry
    var body: some View {
        if (entry.isEmpty && !entry.isPlaceholder) {
            WidgetEmptyView()
        } else {
            switch entry.family {
            case .systemSmall:
                WidgetSmallView(entry: entry)
            case .systemMedium:
                WidgetMediumView(entry: entry)
            case .systemLarge:
                WidgetLargeView(entry: entry)
            default:
                WidgetMediumView(entry: entry)
            }
        }
    }
}

@available(iOS 17.0, *)
struct WidgetLargeView : View {
    let entry: ServerMonitorEntry
    var body: some View {
        WidgetMediumView(entry: entry)
    }
}

@available(iOS 17.0, *)
struct WidgetMediumView : View {
    let entry: ServerMonitorEntry
    var body: some View {
        let maxDisplayPlayers = entry.family == .systemMedium ? 4 : 11
        HStack(spacing: 4.0) {
            WidgetSmallView(entry: entry)
            if (entry.players.count > 0) {
                VStack(alignment: .leading) {
                    ForEach(entry.players.prefix(maxDisplayPlayers)) { player in
                        Text(convertToColourText(value: player.name))
                            .font(.caption)
                            .fontWeight(.regular)
                            .multilineTextAlignment(.leading)
                            .lineLimit(1)
                        Divider()
                    }
                    if (entry.players.count > maxDisplayPlayers) {
                        Text("...")
                            .font(.caption)
                            .fontWeight(.regular)
                        Spacer()
                    } else {
                        Spacer()
                    }
                }
            }
        }
    }
}

@available(iOS 17.0, *)
struct WidgetSmallView : View {
    let entry: ServerMonitorEntry
    var body: some View {
        VStack(alignment: .leading, spacing: 4.0) {
            Text(convertToColourText(value: entry.serverName))
                .font(/*@START_MENU_TOKEN@*/.title2/*@END_MENU_TOKEN@*/)
                .fontWeight(.regular)
                .multilineTextAlignment(.leading)
                .lineLimit(1)
            Text(String(format: "%d/%d players", entry.players.count, entry.maxPlayers))
                .font(.subheadline)
                .fontWeight(.regular)
                .multilineTextAlignment(.leading)
                .lineLimit(1)
            Text(convertToColourText(value: entry.mapName))
                .font(.subheadline)
                .fontWeight(.regular)
                .multilineTextAlignment(.leading)
                .lineLimit(1)
            Spacer()
                .frame(maxWidth: .infinity, maxHeight: .infinity)
            Button("Refresh", intent: RefreshIntent())
                .tint(.mint)
        }
    }
}

@available(iOS 17.0, *)
struct WidgetEmptyView : View {
    var body: some View {
        Text("Long tap to select a server")
            .multilineTextAlignment(.center)
    }
}

@available(iOS 17.0, *)
struct RefreshIntent: AppIntent {
    static var title: LocalizedStringResource = "Refresh"
    
    init() {}
    
    func perform() async throws -> some IntentResult {
//        WidgetCenter.shared.reloadTimelines(ofKind: ServerMonitorWidget.kind)
        return .result()
    }
}

@available(iOS 17.0, *)
struct ServerMonitorWidget: Widget {
    static let kind: String = "Widget"

    var body: some WidgetConfiguration {
        AppIntentConfiguration(kind: ServerMonitorWidget.kind, intent: ConfigurationAppIntent.self, provider: Provider()) { entry in
            WidgetEntryView(entry: entry)
                .containerBackground(.fill.tertiary, for: .widget)
                .widgetURL(URL(string: "jkchat://widget?address="+entry.address))
        }
        .configurationDisplayName("Server monitor")
        .description("Monitors server status")
        .supportedFamilies([.systemSmall, .systemMedium, .systemLarge])
    }
}

@available(iOS 17.0, *)
#Preview(as: .systemMedium) {
    ServerMonitorWidget()
} timeline: {
    ServerMonitorEntry(date: .now, configuration: ConfigurationAppIntent(), serverName: "Refresh2-PUG Refresh2-PUG", players: [ServerMonitorPlayer(name: "^3test ^3test^3test ^3test^3test ^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test"), ServerMonitorPlayer(name: "^3test")], maxPlayers: 32, mapName: "MPA/FFA 2", family: .systemMedium)
}
