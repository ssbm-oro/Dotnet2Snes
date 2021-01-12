using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Dotnet2Snes
{
    public class Dotnet2Snes : IDisposable
    {
        /// <summary>
        /// The underlying connection to the Usb2Snes websocket.
        /// Use to access lower level functions not exposed by this API.
        /// </summary>
        public Usb2Snes.Usb2Snes Usb2Snes { get; private set; }

        /// <summary>
        /// Creates a new instance of the Dotnet2Snes class and try to connect
        /// to the webserver.
        /// </summary>
        /// <param name="port">The port that the webserver is running
        /// on (default 8080).</param>
        public Dotnet2Snes(int port = 8080)
        {
            Usb2Snes = new Usb2Snes.Usb2Snes(port);
            Task.Run(Usb2Snes.Connect).Wait();
        }
        
        /// <summary>
        /// Disposes of the Dotnet2Snes class.
        /// </summary>
        public void Dispose()
        {
            Usb2Snes.Dispose();
        }

        /// <summary>
        /// Get a list of SNES devices available on the webserver.
        /// For USB2SNES, this will be a list of COM ports that a SNES was
        /// found on. For Qusb2Snes, this will include emulators connected as
        /// well.
        /// </summary>
        /// <returns>A list of devices.</returns>
        public async Task<List<string> > GetDeviceList()
        {
            return await Usb2Snes.GetDeviceList();
        }

        /// <summary>
        /// Attach to the given device.
        /// </summary>
        /// <param name="snesDevice">A SNES device listed in the call
        /// to GetDeviceList</param>
        /// <returns>Firmware version of the SD2SNES</returns>
        public async Task<string> Attach(string snesDevice)
        {
            await Usb2Snes.Attach(snesDevice);
            var deviceInfo = await Usb2Snes.Info();
            return deviceInfo.First();
        }

        /// <summary>
        /// Tells the SNES to boot the ROM in the given path.
        /// </summary>
        /// <param name="romPath">Path (on the SD Card) of the
        /// ROM to boot.</param>
        /// <returns></returns>
        public async Task BootRom(string romPath)
        {
            await Usb2Snes.Boot(romPath);
        }

        /// <summary>
        /// Moves a file from the SD card in the SNES to the local computer.
        /// </summary>
        /// <param name="snesFilePath">Path to the file on the SD Card</param>
        /// <param name="localFilePath">Local path to save to.</param>
        /// <returns></returns>
        public async Task MoveFromSnes(string snesFilePath, string localFilePath)
        {
            var file = await Usb2Snes.GetFile(snesFilePath);
            File.WriteAllBytes(localFilePath, file);
        }

        /// <summary>
        /// Moves a file from the local computer to the SD card of the SNES.
        /// </summary>
        /// <param name="localFilePath">Local path of the file to send</param>
        /// <param name="snesFilePath">Path on the SD Card to save the
        /// file to.</param>
        /// <returns></returns>
        public async Task MoveToSnes(string localFilePath, string snesFilePath)
        {
            var file = File.ReadAllBytes(localFilePath);
            await Usb2Snes.PutFile(snesFilePath, file);
        }

        /// <summary>
        /// Lists files in the given directory on the SD2SNES
        /// </summary>
        /// <param name="dirPath">The path to list the contents of.</param>
        /// <returns></returns>
        public async Task<(List<string>, List<string>)> List(string dirPath)
        {
            return await Usb2Snes.List(dirPath);
        }

        /// <summary>
        /// Creates a directory on the SD2SNES.
        /// </summary>
        /// <param name="dirPath">Path to create on the SD card.</param>
        /// <returns></returns>
        public async Task MakeDir(string dirPath)
        {
            await Usb2Snes.MakeDir(dirPath);
        }

        /// <summary>
        /// Delete a directory on the SD2SNES
        /// </summary>
        /// <param name="dirPath">Path to delete on the SD card.</param>
        /// <returns></returns>
        public async Task Remove(string dirPath)
        {
            await Usb2Snes.Remove(dirPath);
        }
    }
}
