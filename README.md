# Original project (99% of the work) was done by [Davide Pesce](https://github.com/davidepesce/SharpCL)

## SharpCL and ImageDemo

This is a fork of the original [SharpCL](https://github.com/davidepesce/SharpCL) that targets [OpenCLOn12](https://github.com/microsoft/OpenCLOn12). My goal with this project was to show it is possible to use a Xbox Series X|S GPU as a GPGPU. 

### Steps to recreate
Assuming you have a Xbox in a developer mode and already have Visual Studio 2019 installed
1. Clone the project
2. Build the project
3. Get your hands on the x64 OpenCLOn12 DLLs from https://www.microsoft.com/en-us/p/opencl-and-opengl-compatibility-pack/9nqpsl29bfff (or build it yourself from [here](https://github.com/microsoft/OpenCLOn12))
4. Deploy the .appxbundle for ImageDemo to your dev Xbox
5. Drop the OpenCLOn12 DLLs into the same folder on disk (Xbox Device Portal > File Explorer > Browse > Follow SMB instructions)
6. Run the app
7. Supply or cancel out of image selection for one to be automatically downloaded
8. Click on Blur or Invert repeatedly
9. Notice on Performance tab of the Xbox Device Portal the GPU Engine utilization jumps up to ~2%
