using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Lytec.SerialPort
{
    public class SerialPortInfo
    {
        public string Name { get; }

        public string Caption { get; }

        public string Description { get; }

        public string Tag { get; }

        private SerialPortInfo(string name, string caption = "", string description = "", string tag = "")
        {
            Name = name;
            Caption = caption;
            Description = description;
            Tag = tag ?? name;
        }

        public static readonly Regex Win32SerialPortNameRegex = new Regex(@"^(.*?)\s*\(((COM\d{1,3})[^()]*?)\)$", RegexOptions.Compiled);
        public static IReadOnlyList<SerialPortInfo> GetSerialPorts()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return System.IO.Ports.SerialPort.GetPortNames().Select(n => new SerialPortInfo(n)).ToList();
            using var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");
            var names = System.IO.Ports.SerialPort.GetPortNames().ToDictionary(n => n);
            var captions = searcher.Get().Cast<ManagementBaseObject>().Select(p => p["Caption"].ToString()).ToList();
            return captions
                .Select(c => Win32SerialPortNameRegex.Match(c))
                .Where(m => m.Success)
                .Select(m => new
                {
                    Caption = m.Groups[0].Value,
                    Description = m.Groups[1].Value,
                    Tag = m.Groups[2].Value,
                    Name = m.Groups[3].Value
                })
                .Where(i => names.ContainsKey(i.Name))
                .Select(i => new SerialPortInfo(i.Name, i.Caption, i.Description, i.Tag))
                .ToList()
                .OrderBy(i => Convert.ToInt32(i.Name.Replace("COM", "")))
                .ToList();
        }
    }

}
