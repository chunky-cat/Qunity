![QUNITY](https://github.com/chunky-cat/com.chunkycat.qunity/blob/main/.docs/logo.png?raw=true)

# Qunity
## quake map importer for Unity

Supported Unity Versions:
* 2021.x
* 2022.x
* 2023.x

Supported Render Pipelines:
* Base
* URP
* HDRP
* your own by replacing the base materials

### What it is
Qunity is a Unity package for importing Quake maps made with Trenchbroom etc.
The package also comes with a Quake WAD importer to remove the need for extracting textures.

### Why is it experimental
Qunity is in a pretty early stage and is in active development. But the base functionality should work as expected.
Please file an issue if you run into problems. :-)


## Instructions

#### How to install:

you can install it via Unity's [package manager via gitURL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):
```
https://github.com/chunky-cat/com.chunkycat.qunity.git
```


#### Setup Trenchbroom for Qunity:

* click on `open preferences...` and set the game path for the 
<br>
<img src="https://github.com/chunky-cat/com.chunkycat.qunity/blob/main/.docs/tutorial_images/tb_prefs.png?raw=true" alt="tb new" width="60%" height=60%/>

* Create a new `Generic` map and use the `Valve` format (Standard should also work tho).
<br>
<img src="https://github.com/chunky-cat/com.chunkycat.qunity/blob/main/.docs/tutorial_images/tb_new.png?raw=true" alt="tb new" width="60%" height=60%/>

the default `Generic` gameconfig searches for a folder named `textures` in the game path (your Assets folder).
Place all your mapping textures in there to make them available in Trenchbroom. This is also the default folder Qunity will use to create materials.

NOTE: currently, only png files are supported, this will be extended in the near future.


