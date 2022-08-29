# Zeno-Color-Selector
&nbsp;&nbsp;&nbsp;&nbsp;Extensible and compact color selector implemented in C# with WPF. Works as both standalone (with test application) and as an imported Control.

![Zeno Color Selector - HSL color mode](/media/zeno_color_selector_HSL.png "Zeno Color Selector - HSL color mode")
![Zeno Color Selector - HSV color mode](/media/zeno_color_selector_HSV.png "Zeno Color Selector - HSV color mode")

# Features
- Versatile color selector with modern user interface (UI) and emphasis on user experience (UX)
- Ability to define a custom set of preset colors and save custom colors at runtime
- Supports RGB, HSL, and HSV color modes, with hexadecimal RGBA string support
- Error-checking for input fields with validation feedback
- Minimal code footprint (two main files - ColorSelector.cs and Themes/Generic.xaml)
- Easy to import as a control library into other WPF projects
- Completely customizable, based on ControlTemplates
- Functions well in nearly any window size, from narrow-wdith window to full-screen

# Setup
&nbsp;&nbsp;&nbsp;&nbsp;The repository consists of two Visual Studio projects - the ColorSelector project and a companion test application. Opening the ColorSelector/ColorSelector.sln file in Visual Studio should allow both projects to be built and run. To use the ColorSelector as a Control in another WPF application, add the ColorSelector project to the parent solution in Visual Studio, then add a project reference to the ColorSelector project for any projects that will use the ColorSelector.

# Screenshots
## Condensed Window
![Zeno Color Selector - Condensed](/media/zeno_color_selector_condensed.png "Zeno Color Selector - Condensed")
