# Script for building ColorSelector and the ColorSelector standalone projects
import sys, os, shutil, time

# Clean the bin and obj directories:
def CleanDirectories():
    if os.path.isdir("{0}\\{1}".format(colorSelectorDir, "bin")):
        shutil.rmtree("{0}\\{1}".format(colorSelectorDir, "bin"), ignore_errors=False)
        
    if os.path.isdir("{0}\\{1}".format(colorSelectorDir, "obj")):
        shutil.rmtree("{0}\\{1}".format(colorSelectorDir, "obj"), ignore_errors=False)
        
    if os.path.isdir("{0}\\{1}".format(colorSelectorStandaloneDir, "bin")):
        shutil.rmtree("{0}\\{1}".format(colorSelectorStandaloneDir, "bin"), ignore_errors=False)
        
    if os.path.isdir("{0}\\{1}".format(colorSelectorStandaloneDir, "obj")):
        shutil.rmtree("{0}\\{1}".format(colorSelectorStandaloneDir, "obj"), ignore_errors=False)

# Replaces existing .csproj file assembly version number with versionString
def UpdateCsprojAssemblyVersion(file):
    text = ""
    assemblyVersionLine = ""

    # read file contents:
    with open(file, 'r') as f:
        text = f.readlines()
    
    # find the assembly version line:
    for line in text:
        if "<AssemblyVersion>" in line:
            assemblyVersionLine = line
    
    # if the assembly version line was not found, discontinue:
    if assemblyVersionLine == "":
        print("Warning: could not find AssemblyVersion line in {0}".format(file))
        return

    # get the index of the assembly version line:
    assemblyIndex = text.index(assemblyVersionLine)
    # preserve the indentation of the original assembly version line:
    leadingSpaces = assemblyVersionLine[:assemblyVersionLine.index("<")]

    # replace the current assembly version line with updated version string:
    text.remove(assemblyVersionLine)
    text.insert(assemblyIndex, "{0}<AssemblyVersion>{1}</AssemblyVersion>\n".format(leadingSpaces, versionString))

    # write the updated text:
    with open(file, 'w') as f:
        f.writelines(text)

# Check script arguments:
if len(sys.argv) < 2:
	print("Usage: " + sys.argv[0] + "[version string]")
	sys.exit()

versionString = sys.argv[1]

# Do some basic checks to help prevent unintentional version string:
if len(versionString.split('.')) != 3:
    print("Error: Version string must contain major,minor, and revision numbers separated by '.'")
    sys.exit()

# Check that the script is being run from the respository root:
currentDirectory = os.path.dirname(os.path.realpath(__file__))
currentDirectoryName = os.path.basename(os.path.dirname(__file__))
if currentDirectoryName != "Color Selector":
    print("Error: build script must be run from repository root! Exiting...")
    exit()
    

colorSelectorDir = "{0}\\{1}".format(currentDirectory, "ColorSelector")
colorSelectorStandaloneDir = "{0}\\{1}".format(currentDirectory, "ColorSelectorTestApp")
colorSelectorCsproj = "{0}\\{1}".format(colorSelectorDir, "ColorSelector.csproj")
colorSelectorStandaloneCsproj = "{0}\\{1}".format(colorSelectorStandaloneDir, "ColorSelectorStandalone.csproj")

# Try to remove the bin and obj directories:
print("Cleaning bin and obj directories ...")
CleanDirectories()

# Visual Studio places some type of lock / regenerative mechanism on the bin and obj
# directories, so wait a few seconds and attempt deleting the folders again if they still exist:
time.sleep(3)
CleanDirectories()
print("done.")

# update the version numbers
print("Setting version {0} in .csproj files ...")
UpdateCsprojAssemblyVersion(colorSelectorCsproj)
UpdateCsprojAssemblyVersion(colorSelectorStandaloneCsproj)
print("done.")