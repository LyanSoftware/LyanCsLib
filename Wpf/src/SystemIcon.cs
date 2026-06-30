using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Lytec.Wpf;

public static class SystemIcon
{
    // 1. 定义系统图标ID
    /// <summary>Used by SHGetStockIconInfo to identify which stock system icon to retrieve.</summary>
    /// <remarks>SIID_INVALID, with a value of -1, indicates an invalid **SHSTOCKICONID** value.</remarks>
    public enum SHSTOCKICONID
    {
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DOCNOASSOC.png"::: Document of a type with no associated application.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DOCNOASSOC = 0,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DOCASSOC.jpg"::: Document of a type with an associated application.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DOCASSOC = 1,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_APPLICATION.jpg"::: Generic application with no custom icon.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_APPLICATION = 2,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_FOLDER.jpg"::: Folder (generic, unspecified state).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_FOLDER = 3,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_FOLDEROPEN.jpg"::: Folder (open).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_FOLDEROPEN = 4,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVE525.jpg"::: 5.25-inch disk drive.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVE525 = 5,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVE35.jpg"::: 3.5-inch disk drive.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVE35 = 6,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVEREMOVE.png"::: Removable drive.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVEREMOVE = 7,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVEFIXED.jpg"::: Fixed drive (hard disk).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVEFIXED = 8,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVENET.jpg"::: Network drive (connected).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVENET = 9,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVENETDISABLED.jpg"::: Network drive (disconnected).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVENETDISABLED = 10,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVECD.jpg"::: CD drive.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVECD = 11,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVERAM.jpg"::: RAM disk drive.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVERAM = 12,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_WORLD.jpg"::: The entire network.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_WORLD = 13,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_SERVER.jpg"::: A computer on the network.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_SERVER = 15,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_PRINTER.jpg"::: A local printer or print destination.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_PRINTER = 16,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MYNETWORK.jpg"::: The **Network** virtual folder (<a href="https://docs.microsoft.com/windows/desktop/shell/knownfolderid">FOLDERID_NetworkFolder</a>/<a href="https://docs.microsoft.com/windows/desktop/shell/csidl">CSIDL_NETWORK</a>).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MYNETWORK = 17,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_FIND.jpg"::: The **Search** feature.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_FIND = 22,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_HELP.jpg"::: The **Help and Support** feature.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_HELP = 23,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_SHARE.jpg"::: Overlay for a shared item.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_SHARE = 28,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_LINK.jpg"::: Overlay for a shortcut.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_LINK = 29,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_SLOWFILE.png"::: Overlay for items that are expected to be slow to access.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_SLOWFILE = 30,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_RECYCLER.jpg"::: The Recycle Bin (empty).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_RECYCLER = 31,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_RECYCLERFULL.jpg"::: The Recycle Bin (not empty).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_RECYCLERFULL = 32,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIACDAUDIO.jpg"::: Audio CD media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIACDAUDIO = 40,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_LOCK.jpg"::: Security lock.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_LOCK = 47,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_AUTOLIST.jpg"::: A virtual folder that contains the results of a search.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_AUTOLIST = 49,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_PRINTERNET.jpg"::: A network printer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_PRINTERNET = 50,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_SERVERSHARE.jpg"::: A server shared on a network.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_SERVERSHARE = 51,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_PRINTERFAX.jpg"::: A local fax printer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_PRINTERFAX = 52,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_PRINTERFAXNET.jpg"::: A network fax printer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_PRINTERFAXNET = 53,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_PRINTERFILE.jpg"::: A file that receives the output of a **Print to file** operation.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_PRINTERFILE = 54,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_STACK.jpg"::: A category that results from a **Stack by** command to organize the contents of a folder.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_STACK = 55,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIASVCD.jpg"::: Super Video CD (SVCD) media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIASVCD = 56,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_STUFFEDFOLDER.jpg"::: A folder that contains only subfolders as child items.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_STUFFEDFOLDER = 57,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVEUNKNOWN.jpg"::: Unknown drive type.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVEUNKNOWN = 58,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVEDVD.jpg"::: DVD drive.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVEDVD = 59,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIADVD.jpg"::: DVD media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIADVD = 60,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIADVDRAM.jpg"::: DVD-RAM media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIADVDRAM = 61,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIADVDRW.jpg"::: DVD-RW media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIADVDRW = 62,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIADVDR.jpg"::: DVD-R media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIADVDR = 63,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIADVDROM.jpg"::: DVD-ROM media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIADVDROM = 64,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIACDAUDIOPLUS.jpg"::: CD+ (enhanced audio CD) media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIACDAUDIOPLUS = 65,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIACDRW.jpg"::: CD-RW media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIACDRW = 66,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIACDR.jpg"::: CD-R media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIACDR = 67,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIACDBURN.jpg"::: A writable CD in the process of being burned.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIACDBURN = 68,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIABLANKCD.jpg"::: Blank writable CD media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIABLANKCD = 69,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIACDROM.jpg"::: CD-ROM media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIACDROM = 70,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_AUDIOFILES.jpg"::: An audio file.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_AUDIOFILES = 71,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_IMAGEFILES.jpg"::: An image file.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_IMAGEFILES = 72,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_VIDEOFILES.jpg"::: A video file.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_VIDEOFILES = 73,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MIXEDFILES.jpg"::: A mixed file.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MIXEDFILES = 74,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_FOLDERBACK.jpg"::: Folder back.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_FOLDERBACK = 75,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_FOLDERFRONT.jpg"::: Folder front.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_FOLDERFRONT = 76,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_SHIELD.jpg"::: Security shield. Use for UAC prompts only.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_SHIELD = 77,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_WARNING.jpg"::: Warning.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_WARNING = 78,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_INFO.jpg"::: Informational.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_INFO = 79,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_ERROR.jpg"::: Error.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_ERROR = 80,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_KEY.jpg"::: Key.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_KEY = 81,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_SOFTWARE.jpg"::: Software.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_SOFTWARE = 82,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_RENAME.jpg"::: A UI item, such as a button, that issues a rename command.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_RENAME = 83,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DELETE.jpg"::: A UI item, such as a button, that issues a delete command.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DELETE = 84,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAAUDIODVD.jpg"::: Audio DVD media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAAUDIODVD = 85,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAMOVIEDVD.jpg"::: Movie DVD media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAMOVIEDVD = 86,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAENHANCEDCD.jpg"::: Enhanced CD media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAENHANCEDCD = 87,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAENHANCEDDVD.jpg"::: Enhanced DVD media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAENHANCEDDVD = 88,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAHDDVD.jpg"::: High definition DVD media in the HD DVD format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAHDDVD = 89,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIABLURAY.jpg"::: High definition DVD media in the Blu-ray Disc™ format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIABLURAY = 90,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAVCD.jpg"::: Video CD (VCD) media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAVCD = 91,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIADVDPLUSR.jpg"::: DVD+R media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIADVDPLUSR = 92,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIADVDPLUSRW.jpg"::: DVD+RW media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIADVDPLUSRW = 93,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DESKTOPPC.jpg"::: A desktop computer.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DESKTOPPC = 94,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MOBILEPC.jpg"::: A mobile computer (laptop).</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MOBILEPC = 95,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_USERS.jpg"::: The **User Accounts** Control Panel item.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_USERS = 96,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIASMARTMEDIA.jpg"::: Smart media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIASMARTMEDIA = 97,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIACOMPACTFLASH.jpg"::: CompactFlash media.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIACOMPACTFLASH = 98,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DEVICECELLPHONE.jpg"::: A cell phone.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DEVICECELLPHONE = 99,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DEVICECAMERA.jpg"::: A digital camera.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DEVICECAMERA = 100,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DEVICEVIDEOCAMERA.jpg"::: A digital video camera.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DEVICEVIDEOCAMERA = 101,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DEVICEAUDIOPLAYER.jpg"::: An audio player.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DEVICEAUDIOPLAYER = 102,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_NETWORKCONNECT.jpg"::: Connect to network.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_NETWORKCONNECT = 103,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_INTERNET.jpg"::: The **Network and Internet** Control Panel item.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_INTERNET = 104,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_ZIPFILE.jpg"::: A compressed file with a .zip file name extension.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_ZIPFILE = 105,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_SETTINGS.jpg"::: The **Additional Options** Control Panel item.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_SETTINGS = 106,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVEHDDVD.jpg"::: **Windows Vista with Service Pack 1 (SP1) and later**. High definition DVD drive (any type - HD DVD-ROM, HD DVD-R, HD-DVD-RAM) that uses the HD DVD format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVEHDDVD = 132,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_DRIVEBD.jpg"::: **Windows Vista with SP1 and later**. High definition DVD drive (any type - BD-ROM, BD-R, BD-RE) that uses the Blu-ray Disc format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_DRIVEBD = 133,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAHDDVDROM.jpg"::: **Windows Vista with SP1 and later**. High definition DVD-ROM media in the HD DVD-ROM format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAHDDVDROM = 134,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAHDDVDR.jpg"::: **Windows Vista with SP1 and later**. High definition DVD-R media in the HD DVD-R format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAHDDVDR = 135,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIAHDDVDRAM.jpg"::: **Windows Vista with SP1 and later**. High definition DVD-RAM media in the HD DVD-RAM format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIAHDDVDRAM = 136,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIABDROM.jpg"::: **Windows Vista with SP1 and later**. High definition DVD-ROM media in the Blu-ray Disc BD-ROM format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIABDROM = 137,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIABDR.jpg"::: **Windows Vista with SP1 and later**. High definition write-once media in the Blu-ray Disc BD-R format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIABDR = 138,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_MEDIABDRE.jpg"::: **Windows Vista with SP1 and later**. High definition read/write media in the Blu-ray Disc BD-RE format.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_MEDIABDRE = 139,
        /// <summary>
        /// <para>:::image type="icon" source="./images/SIID_CLUSTEREDDRIVE.jpg"::: **Windows Vista with SP1 and later**. A cluster disk array.</para>
        /// <para><see href="https://learn.microsoft.com/windows/win32/api/shellapi/ne-shellapi-shstockiconid#members">Read more on docs.microsoft.com</see>.</para>
        /// </summary>
        SIID_CLUSTEREDDRIVE = 140,
        /// <summary>The highest valid value in the enumeration. Values over 160 are Windows 7-only icons.</summary>
        SIID_MAX_ICONS = 181,
    }

    // 2. 定义获取图标信息的标志
    [Flags]
    public enum SHGSI : uint
    {
        SHGSI_ICON = 0x000000100,
        SHGSI_SMALLICON = 0x000000001,
        SHGSI_LARGEICON = 0x000000000,
    }

    // 3. 定义SHGetStockIconInfo函数所需的结构体
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct SHSTOCKICONINFO
    {
        public uint cbSize;
        public IntPtr hIcon;
        public int iSysImageIndex;
        public int iIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szPath;
    }

    // 4. 从DLL中导入API函数
    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern int SHGetStockIconInfo(SHSTOCKICONID siid, SHGSI uFlags, ref SHSTOCKICONINFO psii);

    // 5. 导入销毁图标的API，用于释放非托管资源
    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool DestroyIcon(IntPtr hIcon);

    public static ImageSource? GetIcon(SHSTOCKICONID id)
    {
        var sii = new SHSTOCKICONINFO
        {
            cbSize = (uint)Marshal.SizeOf(typeof(SHSTOCKICONINFO))
        };

        // 调用API获取图标，此处请求小图标
        int hr = SHGetStockIconInfo(id, SHGSI.SHGSI_ICON | SHGSI.SHGSI_SMALLICON, ref sii);

        if (hr == 0 && sii.hIcon != IntPtr.Zero)
        {
            // 将Win32的HICON转换为WPF的ImageSource
            ImageSource shieldSource = Imaging.CreateBitmapSourceFromHIcon(
                sii.hIcon,
                System.Windows.Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            // 重要：释放非托管图标资源，防止内存泄漏
            DestroyIcon(sii.hIcon);

            return shieldSource;
        }
        return null;
    }

    public static ImageSource? GetUACShieldIcon()
    {
        // 确保在Windows Vista (内核版本6.0) 及以上系统运行
        if (Environment.OSVersion.Version.Major < 6)
            return null;

        return GetIcon(SHSTOCKICONID.SIID_SHIELD);
    }
}
