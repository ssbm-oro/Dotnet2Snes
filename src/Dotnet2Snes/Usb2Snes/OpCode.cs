using System.Collections.Generic;
using Newtonsoft.Json;

namespace Dotnet2Snes.Usb2Snes
{    public class Command
    {
        public string Opcode { get; private set; }
        public string Space { get; private set; }
        public List<string> Flags { get; set; }
        public List<string> Operands { get; set; }

        public Command(OpCodes opcode, List<string> flags,
            List<string> operands)
        {
            this.Opcode = opcode.ToString("g");
            this.Space = "SNES";
            this.Flags = flags;
            this.Operands = operands;
        }

        public string ToJson()
        {
            JsonSerializerSettings s = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(this, s);
        }

        public byte[] ToBytes()
        {
            var json = this.ToJson();
            return System.Text.Encoding.UTF8.GetBytes(json);
        }
    }
    
    public enum OpCodes
    {
        // Connection
        DeviceList, // List the available Device {portdevice1, portdevice2, portdevice3...}
        Attach, // Attach to the devise using the name [portdevice]
        AppVersion, // Give the version of the App {version}
        Name, // Specificy the name of the client [name]
        Close, // TODO this close the connection server side


        // Special
        Info, // Give information about the sd2snes firmware {firmwareversion, versionstring, romrunning, flag1, flag2...}
        Boot, // Boot a rom [romname]
        Menu, // Get back to the menu
        Reset, // Reset
        Binary, // UNIMPLEMENTED Send data directly to the sd2snes I guess?
        Stream, // UNIMPLEMENTED - Behavior undefined in Usb2Snes protocol
        Fence, // UNIMPLEMENTED - Behavior undefined in Usb2Snes protocol

        GetAddress, // Get the value of the address, space is important [offset, size]->datarequested
        PutAddress, // put value to the address  [offset, size] then send the binary data.
                    // Also support multiple request in one [offset1, size1, offset2, size2
        PutIPS, // Apply a patch - [name, size] then send binary data
                // a special name is 'hook' for the sd2snes

        GetFile, // Get a file - [filepath]->{size}->filedata
        PutFile, // Post a file -  [filepath, size] then send the binary data
        List, // LS command - [dirpath]->{typefile1, namefile1, typefile2, namefile2...}
        Remove, // remove a file [filepath]
        Rename, // rename a file [filepath, newfilename]
        MakeDir // create a directory [dirpath]
    }
}