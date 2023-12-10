import WidgetKit
import AppIntents

@available(iOS 17.0, *)
struct ConfigurationAppIntent: WidgetConfigurationIntent {
    static var title: LocalizedStringResource = "Select server"
    static var description = IntentDescription("Selects a server to monitor")

    // An example configurable parameter.
    @Parameter(title: "Server")
    var server: ServerEntity
}

@available(iOS 17.0, *)
struct ServerEntity: AppEntity, Decodable {
    static var defaultQuery = ServerEntityQuery()
    
    static var typeDisplayRepresentation: TypeDisplayRepresentation = "Change server"
    
    var displayRepresentation: DisplayRepresentation {
        DisplayRepresentation(title: "\(serverName)")
    }
    
    let address: String
    let port: UInt16
    var id: String {
        get {
            address + ":" + String(port)
        }
    }
    let serverName: String
    var isEmpty: Bool = false
    
    static let empty: ServerEntity = ServerEntity(address: "0.0.0.0", port: 0, serverName: "Choose server", isEmpty: true)
    
    static let servers: [ServerEntity] = [
        ServerEntity(address: "132.145.168.101", port: 29070, serverName: ".........JA+ Official n°1 CTF Get JA+ Plugin & Maps at www.jactf.com, USA"),
        ServerEntity(address: "135.125.145.49", port: 29070, serverName: "{JoF}Public Server"),
        ServerEntity(address: "132.145.168.101", port: 29071, serverName: "Refresh1-PUG"),
        ServerEntity(address: "135.180.64.82", port: 29070, serverName: "Refresh2 PUG")
    ]
    
    static func arrayFromString(json: String) -> [ServerEntity] {
        do {
            guard let data = json.data(using: .utf8) else {
                return []
            }
            let result = try JSONDecoder().decode([ServerEntity].self, from: data)
            return result
        } catch {
            print(error)
        }
        return []
    }
    
    private enum CodingKeys: String, CodingKey {
        case address, port, serverName
    }
}

@available(iOS 17.0, *)
struct ServerEntityQuery: EntityQuery {
    func entities(for identifiers: [ServerEntity.ID]) async throws -> [ServerEntity] {
        var entities = favouritesServers().filter {
            identifiers.contains($0.id)
        }
        if entities.isEmpty {
            entities = [ServerEntity.empty].filter {
                identifiers.contains($0.id)
            }
        }
        return entities
    }
    
    func suggestedEntities() async throws -> [ServerEntity] {
        favouritesServers()
    }
    
    func defaultResult() async -> ServerEntity? {
        ServerEntity.empty
    }
    
    func favouritesServers(empty: Bool = false) -> [ServerEntity] {
        var entities: [ServerEntity] = [ServerEntity.empty]
        guard let userDefaults = UserDefaults(suiteName: "group.com.vlbor.JKChat") else {
            return entities
        }
//        print(userDefaults.object(forKey: "FavouritesServers") != nil)
        guard let favouritesServers = userDefaults.string(forKey: "FavouritesServers") else {
            return entities
        }
        print(favouritesServers)
        entities = ServerEntity.arrayFromString(json: favouritesServers)
        if entities.isEmpty {
            entities = [ServerEntity.empty]
        }
        return entities
    }
}
