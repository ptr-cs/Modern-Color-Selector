# Zeno-Color-Selector
&nbsp;&nbsp;&nbsp;&nbsp;Extensible and compact color selector implemented in C# with WPF. Features highly-customizable default ControlTemplate and interactive 3D color models (:sunglasses:). Works as both standalone (with test application) and as an imported Control. 

![Zeno Color Selector - Short demo](/media/zeno_color_selector.gif "Zeno Color Selector - Short demo")
![Zeno Color Selector - Menu demo](/media/zeno_color_selector_menu_demo.gif "Zeno Color Selector - Menu demo")

# Features
- Versatile color selector with modern user interface (UI) and emphasis on user experience (UX)
- 3D interactive RGB and HSV color models with click-drag and mouse-wheel support for edit actions.
- Menu with options to control visibility of individual color editor components
- Clipboard paste support for hexadecimal color strings - paste directly into the Control at any location
- Ability to define a custom set of preset colors and save custom colors at runtime
- Supports RGB, HSL, and HSV color modes, with hexadecimal RGBA string support
- Error-checking for input fields with validation feedback
- Minimal code footprint (two main files - ColorSelector.cs and Themes/Generic.xaml)
- Easy to import as a control library into other WPF projects
- Completely customizable, based on ControlTemplates
- Functions well in nearly any window size, from narrow-wdith window to full-screen

# Setup
&nbsp;&nbsp;&nbsp;&nbsp;The repository consists of two Visual Studio projects - the ColorSelector project and a companion test application. Opening the ColorSelector/ColorSelector.sln file in Visual Studio should allow both projects to be built and run. To use the ColorSelector as a Control in another WPF application, add the ColorSelector project to the parent solution in Visual Studio, then add a project reference to the ColorSelector project for any projects that will use the ColorSelector.

# In-Progress
- HSV Cone model and integration

# Screenshots
## Normal Width, Wide, and Condensed Windows

![Zeno Color Selector - HSL color mode](/media/zeno_color_selector_HSL.png "Zeno Color Selector - HSL color mode")
![Zeno Color Selector - Wide Window](/media/zeno_color_selector_large_window.png "Zeno Color Selector - Wide Window")
![Zeno Color Selector - Condensed Window](/media/zeno_color_selector_condensed.png "Zeno Color Selector - Condensed Window")

## Menu

![Zeno Color Selector - Menu](/media/zeno_color_selector_menu.png "Zeno Color Selector - Menu")