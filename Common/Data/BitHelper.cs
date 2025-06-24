namespace Lytec.Common.Data
{
    public static class BitHelper
    {
        public static int GetValueEx(int value, int bitOffset, int mask) => (value >> bitOffset) & mask;
        public static int SetValueEx(int oldValue, int newValue, int bitOffset, int mask) => (oldValue & ~(mask << bitOffset)) | ((newValue & mask) << bitOffset);
        public static int GetValue(int value, int bitOffset, int bitWidth) => GetValueEx(value, bitOffset, (int)MakeMask(bitWidth));
        public static int SetValue(int oldValue, int newValue, int bitOffset, int bitWidth) => SetValueEx(oldValue, newValue, bitOffset, (int)MakeMask(bitWidth));
        public static bool GetFlag(int value, int bitOffset) => GetValueEx(value, bitOffset, 1) != 0;
        public static int SetFlag(int oldValue, bool flag, int bitOffset) => SetValueEx(oldValue, flag ? 1 : 0, bitOffset, 1);
        public static long MakeMask(int bitWidth) => (1 << bitWidth) - 1;
        public static long GetValueEx(long value, int bitOffset, long mask) => (value >> bitOffset) & mask;
        public static long SetValueEx(long oldValue, long newValue, int bitOffset, long mask) => (oldValue & ~(mask << bitOffset)) | ((newValue & mask) << bitOffset);
        public static long GetValue(long value, int bitOffset, int bitWidth) => GetValueEx(value, bitOffset, MakeMask(bitWidth));
        public static long SetValue(long oldValue, long newValue, int bitOffset, int bitWidth) => SetValueEx(oldValue, newValue, bitOffset, MakeMask(bitWidth));
        public static bool GetFlag(long value, int bitOffset) => GetValueEx(value, bitOffset, 1) != 0;
        public static long SetFlag(long oldValue, bool flag, int bitOffset) => SetValueEx(oldValue, flag ? 1 : 0, bitOffset, 1);
        public static ulong GetValueEx(ulong value, int bitOffset, ulong mask) => (value >> bitOffset) & mask;
        public static ulong SetValueEx(ulong oldValue, ulong newValue, int bitOffset, ulong mask) => (oldValue & ~(mask << bitOffset)) | ((newValue & mask) << bitOffset);
        public static ulong GetValue(ulong value, int bitOffset, int bitWidth) => GetValueEx(value, bitOffset, (ulong)MakeMask(bitWidth));
        public static ulong SetValue(ulong oldValue, ulong newValue, int bitOffset, int bitWidth) => SetValueEx(oldValue, newValue, bitOffset, (ulong)MakeMask(bitWidth));
        public static bool GetFlag(ulong value, int bitOffset) => GetValueEx(value, bitOffset, 1) != 0;
        public static ulong SetFlag(ulong oldValue, bool flag, int bitOffset) => SetValueEx(oldValue, (ulong)(flag ? 1 : 0), bitOffset, 1);
    }
}
