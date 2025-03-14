namespace WidgetShared {
    using System;
    using Foundation;

    // @interface LiveActivityShared : NSObject
    [BaseType(typeof(NSObject))]
    interface LiveActivityShared
    {
        // +(void)refreshWidgetsOfKind:(NSString * _Nullable)ofKind;
        [Static]
        [Export("refreshWidgetsOfKind:")]
        void RefreshWidgetsOfKind([NullAllowed] string ofKind);

        // +(void)showLiveActivityWithServers:(NSArray<NSString *> * _Nonnull)servers messages:(uint32_t)messages players:(uint32_t)players maxPlayers:(uint32_t)maxPlayers disconnectHandler:(void (^ _Nonnull)(void))disconnectHandler completionHandler:(void (^ _Nonnull)(void))completionHandler;
        [Static]
        [Export("showLiveActivityWithServers:messages:players:maxPlayers:disconnectHandler:completionHandler:")]
        void ShowLiveActivityWithServers(string[] servers, uint messages, uint players, uint maxPlayers, Action disconnectHandler, Action completionHandler);

        // +(void)stopLiveActivityWithCompletionHandler:(void (^ _Nonnull)(void))completionHandler;
        [Static]
        [Export("stopLiveActivityWithCompletionHandler:")]
        void StopLiveActivity(Action completionHandler);
    }
}