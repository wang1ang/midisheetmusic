/*
 * Copyright (c) 2009-2011 Madhav Vaidyanathan
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License version 2.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 */

using System; 
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices; 

/** @class Volume
 *  This volume class has one simple static method:
 *  void SetVolume(int value)
 *  where value is between 0 and 100.
 *
 */
namespace MidiSheetMusic {


/* Windows XP uses the MIXERCONTROL structs to set the Volume */
public struct MIXERCONTROLDETAILS
{
    public int cbStruct;
    public int dwControlID;
    public int cChannels;
    public int item;
    public int cbDetails;
    public IntPtr paDetails;
}

public struct MIXERCONTROLDETAILS_UNSIGNED
{
    public int dwValue;
}


/* Windows Vista uses several COM interfaces to set the Volume. */

[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]

public interface IAudioEndpointVolume {
    int RegisterControlChangeNotify(IntPtr p);
    int UnregisterControlChangeNotify(IntPtr p);
    int GetChannelCount(ref uint channelCount);
    int SetMasterVolumeLevel(float levelDB, Guid context); 
    int SetMasterVolumeLevelScalar(float level, Guid context);
    int GetMasterVolumeLevel(ref float levelDB);
    int GetMasterVolumeLevelScalar(ref float level);
    int SetChannelVolumeLevel(uint channel, float levelDB, Guid context);
    int SetChannelVolumeLevelScalar(uint channel, float level, Guid context);
    int GetChannelVolumeLevel(uint channel, ref float leveldb);
    int GetChannelVolumeLevelScalar(uint channel, ref float level);
    int SetMute(bool mute, Guid context);
    int GetMute(ref bool mute);
    int GetVolumeStepInfo(ref uint step, ref uint stepCount);
    int VolumeStepUp(Guid context);
    int VolumeStepDown(Guid context);
    int QueryHardwareSupport(ref uint mask);
    int GetVolumeRange(ref float min, ref float max, ref float inc);
}

[Guid("D666063F-1587-4E43-81F1-B948E807363F"),
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDevice {
    int Activate(ref Guid id, uint context, IntPtr p, ref IntPtr endpoint);
    int OpenPropertyStore(int access, ref IntPtr properties);
    int GetId(ref string id);
    int GetState(ref int state);
}


[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), 
 InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IMMDeviceEnumerator {
    int EnumAudioEndpoints(int dataFlow, int dwStateMask, ref IntPtr devices);
    int GetDefaultAudioEndpoint(int dataFlow, int role, ref IntPtr device);
    int GetDevice(string id, ref IntPtr device);
    int RegisterEndpointNotificationCallback(IntPtr client);
    int UnregisterEndpointNotificationCallback(IntPtr client);
}


public class Volume {
    public const int MIXER_SETCONTROLDETAILSF_VALUE = 0x0;

    /* Win32 API audio function prototypes  */
    [DllImport("winmm.dll")]
    private static extern int mixerOpen(out int mix, int mixerId, int callback, int instance, int flags);

    [DllImport("winmm.dll")]
    private static extern int mixerClose(int handle);

    [DllImport("winmm.dll")]
    private static extern int 
    mixerSetControlDetails(int handle, ref MIXERCONTROLDETAILS details, int fdwDetails);

    [DllImport("ole32.dll")]
    private static extern int 
    CoCreateInstance(ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] object inner, 
                     int context, ref Guid iid, 
                     [MarshalAs(UnmanagedType.IUnknown)] out object comObject);

    [DllImport("ole32.dll")]
    private static extern int CoInitialize(object arg);


    /* Set the Volume on Windows Vista by obtaining an
     * IAudioEndpointVolume COM object. 
     */
    public static void SetVolumeWindowsVista(int value) {
        object enumerator = null;
        Guid devEnumeratorGuid = new Guid("BCDE0395-E52F-467C-8E3D-C4579291692E");
        Guid idevEnumeratorGuid = new Guid("00000000-0000-0000-C000-000000000046");
        int CLSCTX_INPROC_SERVER = 1;

        CoInitialize(null);
        int result = CoCreateInstance(ref devEnumeratorGuid, null, 
                                       CLSCTX_INPROC_SERVER, ref idevEnumeratorGuid, 
                                       out enumerator);
        if (result != 0 || enumerator == null) {
            return;
        }
        IMMDeviceEnumerator devEnum = enumerator as IMMDeviceEnumerator;
        if (devEnum == null) {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(devEnum);
            return;
        }
        IntPtr devicePtr = IntPtr.Zero;
        int eRender = 0; int eConsole = 0;
        int ret = devEnum.GetDefaultAudioEndpoint(eRender, eConsole, ref devicePtr);
        if (ret != 0) {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(devEnum);
            return;
        }
        object obj = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(devicePtr);
        IMMDevice device = obj as IMMDevice;
        if (device == null) {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(devEnum);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(device);
        }
        Guid iAudioEndPointVolGuid = new Guid("5CDF2C82-841E-4546-9722-0CF74078229A");
        uint context = (uint)0x17;
        IntPtr activationParams = IntPtr.Zero;
        IntPtr endPoint = IntPtr.Zero;
        ret = device.Activate(ref iAudioEndPointVolGuid, context, 
                              activationParams, ref endPoint);
        if (ret != 0) {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(devEnum);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(device);
            return;
        }
        obj = System.Runtime.InteropServices.Marshal.GetObjectForIUnknown(endPoint);
        IAudioEndpointVolume audioEndpoint = obj as IAudioEndpointVolume;
        if (audioEndpoint == null) {
            System.Runtime.InteropServices.Marshal.ReleaseComObject(devEnum);
            System.Runtime.InteropServices.Marshal.ReleaseComObject(device);
            return;
        }
        Guid empty = Guid.Empty;
        audioEndpoint.SetMasterVolumeLevelScalar((float)(value/100.0), empty);
        System.Runtime.InteropServices.Marshal.ReleaseComObject(audioEndpoint);
        System.Runtime.InteropServices.Marshal.ReleaseComObject(device);
        System.Runtime.InteropServices.Marshal.ReleaseComObject(devEnum);
    }


    /* Set the SW Synth volume on Windows XP, by using the
     * Win32 API function mixerSetControlDetails().
     */
    public static void SetVolumeWindowsXP(int value) {
        /* The actual master volume range is 0 to 65536. Scale the value */
        value = (int)(value * 65536 / 100.0);

        int mixerHandle;
        int ret = mixerOpen(out mixerHandle, 0, 0, 0, 0);

        MIXERCONTROLDETAILS details = new MIXERCONTROLDETAILS();
        MIXERCONTROLDETAILS_UNSIGNED volumeDetails = new MIXERCONTROLDETAILS_UNSIGNED();
        details.item = 0;

        /* The ControlID is which volume to modify:
         *   Master    = 0
         *   Wave      = 2
         *   SW Synth  = 4
         *   CD Player = 6
         */
        details.dwControlID = 4;
        details.cbStruct = Marshal.SizeOf(details);
        details.cbDetails = Marshal.SizeOf(volumeDetails);
        details.cChannels = 1;
        volumeDetails.dwValue = value;

        details.paDetails = Marshal.AllocCoTaskMem(Marshal.SizeOf(typeof(MIXERCONTROLDETAILS_UNSIGNED)));
        Marshal.StructureToPtr(volumeDetails, details.paDetails, false);
        ret = mixerSetControlDetails(mixerHandle, ref details, MIXER_SETCONTROLDETAILSF_VALUE);
        mixerClose(mixerHandle);
    }

    /* Set the Volume on Linux by using the command:
     * # amixer -c 0 sset Master playback 80%
     * amixer is the command-line mixer for the ALSA driver, found
     * in package alsa-utils on Ubuntu.
     */
    public static void SetVolumeLinux(int value) {
        int dbvalue = (int)( Math.Log10(value) * 100.0 / 2.0 );
        if (value <= 1) {
            dbvalue = 0;
        }
        try {
            ProcessStartInfo info = new ProcessStartInfo();
            info.CreateNoWindow = true;
            info.RedirectStandardOutput = true;
            info.RedirectStandardInput = true;
            info.UseShellExecute = false;
            info.FileName = "/usr/bin/amixer";
            info.Arguments = " -c 0 sset Master playback " + dbvalue + "%";
            Process amixer = new Process();
            amixer.StartInfo = info;
            amixer.Start();
            amixer.WaitForExit();
            amixer.Close(); 
        }
        catch (Exception e) {
        }
    }

    public static void SetVolume(int value) {
        if (Type.GetType("Mono.Runtime") != null) {
            SetVolumeLinux(value);
        }
        else if (Environment.OSVersion.Version.Major == 5) {
            SetVolumeWindowsXP(value);
        }
        else {
            SetVolumeWindowsVista(value);
        }
    }
}

}



