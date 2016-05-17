$AndroidToolPath = "${env:ProgramFiles(x86)}\Android\android-sdk\tools\android" 
#$AndroidToolPath = "$env:localappdata\Android\android-sdk\tools\android"

echo 'y' | & $AndroidToolPath update sdk -u -a -t android-15
echo 'y' | & $AndroidToolPath update sdk -u -a -t addon-google_apis-google-15