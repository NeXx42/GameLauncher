# Runix Launcher
A simple Linux game launcher designed to make managing and launching your games. Integrates with your Steam library and lets you pick the runtime for each game. Uses tags for organisation, and images for customly imported games can be generated used an incorporated overlay.

Built with Avalonia, cross platform. However mainly designed for linux.

## Screenshots
<img width="1671" height="892" alt="image" src="https://github.com/user-attachments/assets/01499658-6e39-4223-bbf6-0b074b4bf097" />
<img width="1921" height="1081" alt="image" src="https://github.com/user-attachments/assets/0a215d25-9bb9-4c11-a589-87af4ef61ba6" />
<img width="1920" height="1081" alt="image" src="https://github.com/user-attachments/assets/f3394608-cb8f-4069-9d20-459ea2d06ec1" />

## Prerequisites

Requires
* .Net
* Rust

## Installation
1. Clone
```sh
git clone https://github.com/NeXx42/RunixLauncher.git --recursive
```
2. Build
```sh
cd ./RunixLauncher
make publish-appimage
```
3. Run
```sh
./Build/Output/RunixLauncher.appimage
```




