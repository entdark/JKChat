import UIKit

@main
class AppDelegate: UIResponder, UIApplicationDelegate {



    func application(_ application: UIApplication, didFinishLaunchingWithOptions launchOptions: [UIApplication.LaunchOptionsKey: Any]?) -> Bool {
        // Override point for customization after application launch.
        guard let userDefaults = UserDefaults(suiteName: "group.com.vlbor.JKChat") else {
            return true
        }
        userDefaults.set("[{\"address\":\"132.145.168.101\",\"port\":29070,\"serverName\":\".........JA+ Official n°1 CTF Get JA+ Plugin & Maps at www.jactf.com, USA\"},{\"address\":\"135.125.145.49\",\"port\":29070,\"serverName\":\"{JoF}Public Server\"},{\"address\":\"132.145.168.101\",\"port\":29071,\"serverName\":\"Refresh1-PUG\"},{\"address\":\"135.180.64.82\",\"port\":29070,\"serverName\":\"Refresh2 PUG\"}]", forKey: "FavouritesServers")
        return true
    }

    // MARK: UISceneSession Lifecycle

    func application(_ application: UIApplication, configurationForConnecting connectingSceneSession: UISceneSession, options: UIScene.ConnectionOptions) -> UISceneConfiguration {
        // Called when a new scene session is being created.
        // Use this method to select a configuration to create the new scene with.
        return UISceneConfiguration(name: "Default Configuration", sessionRole: connectingSceneSession.role)
    }

    func application(_ application: UIApplication, didDiscardSceneSessions sceneSessions: Set<UISceneSession>) {
        // Called when the user discards a scene session.
        // If any sessions were discarded while the application was not running, this will be called shortly after application:didFinishLaunchingWithOptions.
        // Use this method to release any resources that were specific to the discarded scenes, as they will not return.
    }


}

