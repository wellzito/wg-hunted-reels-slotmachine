// Creating functions for the Unity
mergeInto(LibraryManager.library, {
    OpenURL: function (URL) {
        var convertedText = Pointer_stringify(URL);
        GoToURL(convertedText);
    },
    FullScreen: function () {
        FullScreen();
    },
    IsIOSBrowser: function () {
        return (/iPhone|iPad|iPod/i.test(navigator.userAgent));
      },
      IsAndroidBrowser: function () {
        return (/Android/i.test(navigator.userAgent));
      },
      IsDesktopBrowser__deps: ['IsIOSBrowser', 'IsAndroidBrowser'],
      IsDesktopBrowser: function () {
        return !_IsIOSBrowser() && !_IsAndroidBrowser();
    }
});