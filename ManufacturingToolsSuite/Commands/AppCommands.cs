using System.Windows.Input;

namespace ManufacturingToolsSuite.Commands
{
    public static class AppCommands
    {
        public static readonly RoutedUICommand Home = new(
            text: "Home",
            name: "Home",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D0, ModifierKeys.Control) });

        public static readonly RoutedUICommand Back = new(
            text: "Back",
            name: "Back",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.Back) });

        public static readonly RoutedUICommand BmcAi = new(
            text: "BmcAi",
            name: "BmcAi",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D1, ModifierKeys.Control) });

        public static readonly RoutedUICommand SapExcel = new(
            text: "SapExcel",
            name: "SapExcel",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D2, ModifierKeys.Control) });

        public static readonly RoutedUICommand Nesting = new(
            text: "Nesting",
            name: "Nesting",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D3, ModifierKeys.Control) });

        public static readonly RoutedUICommand StepViewer = new(
            text: "StepViewer",
            name: "StepViewer",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D4, ModifierKeys.Control) });

        public static readonly RoutedUICommand IoAnalyser = new(
            text: "IoAnalyser",
            name: "IoAnalyser",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D5, ModifierKeys.Control) });

        public static readonly RoutedUICommand MakeBuy = new(
            text: "MakeBuy",
            name: "MakeBuy",
            ownerType: typeof(AppCommands),
            inputGestures: new InputGestureCollection { new KeyGesture(Key.D6, ModifierKeys.Control) });
    }
}
