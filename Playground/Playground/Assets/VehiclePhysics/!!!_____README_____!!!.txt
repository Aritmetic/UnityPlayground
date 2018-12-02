[Standard Assets]
For water and trees in demo scene to appear you will need to import "Environment" from Standard Assets - this is not required but you might get a few warnings about missing prefabs.
You can do this by right clicking inside the Project view in the editor > Import Package > Package name.
If you do not want to use the demo scene you do not need to import any standard assets.
In case Standard Assets are not available under right click you will need to install them or download them from Asset Store.

[Input]
Since the desktop input manager uses GetAxis() and GetButton() to avoid hard-coding the inputs, you will also need to set up those.
More about this can be found inside the manual, but for a quick start you can just copy the provided project settings (ProjectSettings.zip) 
into your ProjectSettings directory ([your_project_name]/ProjectSettings). This will overwrite your existing settings.
If you do not set up the inputs desktop input manager will still work but will show warnings and use hard-coded defaults.
