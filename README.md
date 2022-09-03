# Zeno-Color-Selector
&nbsp;&nbsp;&nbsp;&nbsp;Extensible and compact color selector implemented in C# with WPF. Features highly-customizable default ControlTemplate and interactive 3D color models. Works as both standalone (with test application) and as an imported Control. Protoyped with Adobe XD and tested with NUnit unit testing framework.

![Zeno Color Selector - Prototyped UI](/media/zeno_color_selector_ui2.jpg "Zeno Color Selector - Prototyped UI")

![Zeno Color Selector - Cube demo](/media/zeno_color_selector_cube.gif "Zeno Color Selector - Cube demo")
![Zeno Color Selector - Cone demo](/media/zeno_color_selector_cone.gif "Zeno Color Selector - Cone demo")
![Zeno Color Selector - Menu demo](/media/zeno_color_selector_menu.gif "Zeno Color Selector - Menu demo")

# Features
- .JSON file import and export support for loading and saving colors
- Drag-and-drop support for loading .JSON files when drag-dropping over the Presets and Custom color selectors
- Dynamic application scale controls, allowing the user to increase the scale of the entire interface if needed.
- Versatile color selector with modern user interface (UI) and emphasis on user experience (UX)
- 3D interactive RGB cube and HSV cone color models with click-drag and mouse-wheel support for edit actions
- Menu with options to control visibility of individual color editor components
- Clipboard paste support for hexadecimal color strings - paste directly into the Control at any location
- Ability to define a custom set of preset colors and save custom colors at runtime
- Supports RGB, HSL, and HSV color modes, with hexadecimal RGBA string support
- Error-checking for input fields with validation feedback
- Minimal code footprint (two main files - ColorSelector.cs and Themes/Generic.xaml)
- Easy to import as a control library into other WPF projects
- Completely customizable, based on ControlTemplates
- Functions well in nearly any window size, from narrow-wdith window to full-screen

For a more detailed explanation of the code, [a blog post about the project is available here](http://dividebyzeno.com/zeno-color-selector-part1.html).

# Setup
&nbsp;&nbsp;&nbsp;&nbsp;The repository consists of three Visual Studio projects - the ColorSelector project, a companion standalone application project, and an NUnit unit testing project. Opening the ColorSelector/ColorSelector.sln file in Visual Studio should allow all projects to be built and run. To use the ColorSelector as a Control in another WPF application, add the ColorSelector project to the parent solution in Visual Studio, then add a project reference to the ColorSelector project for any projects that will use the ColorSelector.

# In-Progress
- JSON import and export
- Dynamically scalable UI controls

# Screenshots

![Zeno Color Selector - HSL color mode](/media/zeno_color_selector_HSL.png "Zeno Color Selector - HSL color mode")
![Zeno Color Selector - Wide Window](/media/zeno_color_selector_large_window.png "Zeno Color Selector - Wide Window")
![Zeno Color Selector - Condensed Window](/media/zeno_color_selector_condensed.png "Zeno Color Selector - Condensed Window")
![Zeno Color Selector - Menu](/media/zeno_color_selector_menu.png "Zeno Color Selector - Menu")

# Adobe XD Prototype

![Zeno Color Selector - Adobe XD artboards](/media/zeno_color_selector_artboards.jpg "Zeno Color Selector - Adobe XD artboards")
