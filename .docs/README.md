# Quickstart

> Installation

you can install it via Unity's [package manager via gitURL](https://docs.unity3d.com/Manual/upm-ui-giturl.html):
```
https://github.com/chunky-cat/com.chunkycat.qunity.git
```

if you don't have git installed, you can also download and unpack the package via github to install it as a local package.

---

# configure Trenchbroom

#### Install Trenchbroom

Download and install Trnchbroom from the [official site](https://trenchbroom.github.io/)

#### Setup Trenchbroom for Qunity:

* click on `open preferences...` and set the game path for `Generic` to the `Assets` folder of your Unity Project.
* Create a new `Generic` map and use the `Valve` format (Standard should also work tho).
<br>
<img src="_media/trenchbroom/tb_prefs.png?raw=true" alt="tb new" width="60%" height=60%/>
<br>

the default `Generic` gameconfig searches for a folder named `textures` in the game path (your Assets folder).
Place all your mapping textures in there to make them available in Trenchbroom. This is also the default folder Qunity will use to create materials.

#### Learn Trenchbroom:

here are some nice resources that should get you started with Trenchbroom and Leveldesign:

* [Youtube series from dumptruck_ds](https://www.youtube.com/playlist?app=desktop&list=PLey5_iyK0EccGCJtL4h-AL4u67riOn4e3)
* [Leveldesign Book](https://book.leveldesignbook.com/)

---

# importing a map

Qunity's design goal is that it provides an easy yet flexible workflow. once you have your texture folder in place, you can simply save (or drag and drop) your .map file into your `Assets` folder. 


As of now (v1.0.6), The first import could give you a wrning due to texture reimports. Qunity will convert the textures to RGBA32, setup transparent pixels and generate an emmission map. reimporting a texture means that Qunity cannot use it right away. If you see this warning, simply reimport the map using the context menu (right click) on the map file.

<br>
<img src="_media/qunity/qunity_texture_warn.png?raw=true" alt="tb new" width="60%" height=60%/>
<br>
