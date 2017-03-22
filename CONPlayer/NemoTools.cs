using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using NAudio.Midi;
using Encoder = System.Drawing.Imaging.Encoder;

namespace cPlayer
{
    public class NemoTools
    {
        public int TextureSize = 512; //default value
        public bool KeepDDS = false;
        
        /// <summary>
        /// Will safely try to move, and if fails, copy/delete a file
        /// </summary>
        /// <param name="input">Full starting path of the file</param>
        /// <param name="output">Full destination path of the file</param>
        public bool MoveFile(string input, string output)
        {
            try
            {
                DeleteFile(output);
                File.Move(input, output);
            }
            catch (Exception)
            {
                try
                {
                    File.Copy(input, output);
                    DeleteFile(input);
                }
                catch (Exception)
                {
                    return false;
                }
            }
            return File.Exists(output);
        }

        /// <summary>
        /// Simple function to safely delete folders
        /// </summary>
        /// <param name="folder">Full path of folder to be deleted</param>
        /// <param name="delete_contents">Whether to delete folders that are not empty</param>
        public void DeleteFolder(string folder, bool delete_contents)
        {
            if (!Directory.Exists(folder)) return;
            try
            {
                if (delete_contents)
                {
                    Directory.Delete(folder, true);
                    return;
                }
                if (!Directory.GetFiles(folder).Any())
                {
                    Directory.Delete(folder);
                }
            }
            catch (Exception)
            {}
        }

        /// <summary>
        /// Simple function to safely delete files
        /// </summary>
        /// <param name="file">Full path of file to be deleted</param>
        public void DeleteFile(string file)
        {
            if (string.IsNullOrEmpty(file) || !File.Exists(file)) return;
            try
            {
                File.Delete(file);
            }
            catch (Exception)
            { }
        }

        /// <summary>
        /// Simple function to safely delete folders
        /// </summary>
        /// <param name="folder">Full path of folder to be deleted</param>
        public void DeleteFolder(string folder)
        {
            if (!Directory.Exists(folder)) return;
            DeleteFolder(folder, false);
        }
        
        private static byte[] BuildDDSHeader(string format, int width, int height)
        {
            var dds = new byte[] //512x512 DXT5
                {
                    0x44, 0x44, 0x53, 0x20, 0x7C, 0x00, 0x00, 0x00, 0x07, 0x10, 0x0A, 0x00, 0x00, 0x02, 0x00, 0x00, 
                    0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x4E, 0x45, 0x4D, 0x4F, 0x00, 0x00, 0x00, 0x00, 0x20, 0x00, 0x00, 0x00, 
                    0x04, 0x00, 0x00, 0x00, 0x44, 0x58, 0x54, 0x35, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x10, 0x00, 0x00, 
                    0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
                };

            switch (format.ToLowerInvariant())
            {
                case "dxt1":
                    dds[87] = 0x31;
                    break;
                case "dxt3":
                    dds[87] = 0x33;
                    break;
                case "normal":
                    dds[84] = 0x41;
                    dds[85] = 0x54;
                    dds[86] = 0x49;
                    dds[87] = 0x32;
                    break;
            }

            switch (height)
            {
                case 8:
                    dds[12] = 0x08;
                    dds[13] = 0x00;
                    break;
                case 16:
                    dds[12] = 0x10;
                    dds[13] = 0x00;
                    break;
                case 32:
                    dds[12] = 0x20;
                    dds[13] = 0x00;
                    break;
                case 64:
                    dds[12] = 0x40;
                    dds[13] = 0x00;
                    break;
                case 128:
                    dds[12] = 0x80;
                    dds[13] = 0x00;
                    break;
                case 256:
                    dds[13] = 0x01;
                    break;
                case 1024:
                    dds[13] = 0x04;
                    break;
                case 2048:
                    dds[13] = 0x08;
                    break;
            }

            switch (width)
            {
                case 8:
                    dds[16] = 0x08;
                    dds[17] = 0x00;
                    break;
                case 16:
                    dds[16] = 0x10;
                    dds[17] = 0x00;
                    break;
                case 32:
                    dds[16] = 0x20;
                    dds[17] = 0x00;
                    break;
                case 64:
                    dds[16] = 0x40;
                    dds[17] = 0x00;
                    break;
                case 128:
                    dds[16] = 0x80;
                    dds[17] = 0x00;
                    break;
                case 256:
                    dds[17] = 0x01;
                    break;
                case 1024:
                    dds[17] = 0x04;
                    break;
                case 2048:
                    dds[17] = 0x08;
                    break;
            }
            return dds;
        }
        
        /// <summary>
        /// Figure out right DDS header to go with HMX texture
        /// </summary>
        /// <param name="full_header">First 16 bytes of the png_xbox/png_ps3 file</param>
        /// <param name="short_header">Bytes 5-16 of the png_xbox/png_ps3 file</param>
        /// <returns></returns>
        private static byte[] GetDDSHeader(byte[] full_header, byte[] short_header)
        {
            //official album art header, most likely to be the one being requested
            var header = BuildDDSHeader("dxt1", 256, 256);

            var headers = Directory.GetFiles(Application.StartupPath + "\\bin\\headers\\", "*.header");
            foreach (var head in headers)
            {
                var header_bytes = File.ReadAllBytes(head);
                if (!full_header.SequenceEqual(header_bytes) && !short_header.SequenceEqual(header_bytes)) continue;

                var head_name = Path.GetFileNameWithoutExtension(head).ToLowerInvariant();
                var format = "dxt5";
                if (head_name.Contains("dxt1"))
                {
                    format = "dxt1";
                }
                else if (head_name.Contains("normal"))
                {
                    format = "normal";
                }

                var index1 = head_name.IndexOf("_", StringComparison.Ordinal) + 1;
                var index2 = head_name.IndexOf("x", StringComparison.Ordinal);
                var width = Convert.ToInt16(head_name.Substring(index1, index2 - index1));
                index1 = head_name.IndexOf("_", index2, StringComparison.Ordinal);
                index2++;
                var height = Convert.ToInt16(head_name.Substring(index2, index1 - index2));

                header = BuildDDSHeader(format, width, height);
                break;
            }
            return header;
        }
        
        /// <summary>
        /// Converts png_xbox files to usable format
        /// </summary>
        /// <param name="rb_image">Full path to the png_xbox / png_ps3 / dds file</param>
        /// <param name="output_path">Full path you'd like to save the converted image</param>
        /// <param name="format">Allowed formats: BMP | JPG | PNG (default)</param>
        /// <param name="delete_original">True: delete | False: keep (default)</param>
        /// <returns></returns>
        public bool ConvertRBImage(string rb_image, string output_path, string format, bool delete_original)
        {
            var ddsfile = Path.GetDirectoryName(output_path) + "\\" + Path.GetFileNameWithoutExtension(output_path) + ".dds";
            var tgafile = ddsfile.Replace(".dds", ".tga");
            
            var nv_tool = Application.StartupPath + "\\bin\\nvdecompress.exe";
            if (!File.Exists(nv_tool))
            {
                MessageBox.Show("nvdecompress.exe is missing and is required\nProcess aborted", "Nemo Tools",MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            try
            {
                DeleteFile(ddsfile);
                DeleteFile(tgafile);

                //read raw file bytes
                var ddsbytes = File.ReadAllBytes(rb_image);

                var buffer = new byte[4];
                var swap = new byte[4];

                //get filesize / 4 for number of times to loop
                //32 is the size of the HMX header to skip
                var loop = (ddsbytes.Length - 32) / 4;

                //skip the HMX header
                var input = new MemoryStream(ddsbytes, 32, ddsbytes.Length - 32);

                //grab HMX header to compare against known headers
                var full_header = new byte[16];
                var file_header = new MemoryStream(ddsbytes, 0, 16);
                file_header.Read(full_header, 0, 16);
                file_header.Dispose();

                //some games have a bunch of headers for the same files, so let's skip the varying portion and just
                //grab the part that tells us the dimensions and image format
                var short_header = new byte[11];
                file_header = new MemoryStream(ddsbytes, 5, 11);
                file_header.Read(short_header, 0, 11);
                file_header.Dispose();

                //create dds file
                var output = new FileStream(ddsfile, FileMode.Create);
                var header = GetDDSHeader(full_header, short_header);
                output.Write(header, 0, header.Length);

                //here we go
                for (var x = 0; x <= loop; x++)
                {
                    input.Read(buffer, 0, 4);

                    //PS3 images are not byte swapped, just DDS images with HMX header on top
                    if (rb_image.EndsWith("_ps3", StringComparison.Ordinal))
                    {
                        swap = buffer;
                    }
                    else
                    {
                        //XBOX images are byte swapped, so we gotta return it
                        swap[0] = buffer[1];
                        swap[1] = buffer[0];
                        swap[2] = buffer[3];
                        swap[3] = buffer[2];
                    }
                    output.Write(swap, 0, 4);
                }
                input.Dispose();
                output.Dispose();

                //read raw dds bytes
                ddsbytes = File.ReadAllBytes(ddsfile);

                //grab relevant part of dds header
                var header_stream = new MemoryStream(ddsbytes, 0, 32);
                var size = new byte[32];
                header_stream.Read(size, 0, 32);
                header_stream.Dispose();

                //default to 256x256
                var width = 256;

                //get dds dimensions from header
                switch (size[17]) //width byte
                {
                    case 0x00:
                        switch (size[16])
                        {
                            case 0x08:
                                width = 8;
                                break;
                            case 0x10:
                                width = 16;
                                break;
                            case 0x20:
                                width = 32;
                                break;
                            case 0x40:
                                width = 64;
                                break;
                            case 0x80:
                                width = 128;
                                break;
                        }
                        break;
                    case 0x02:
                        width = 512;
                        break;
                    case 0x04:
                        width = 1024;
                        break;
                    case 0x08:
                        width = 2048;
                        break;
                }
                TextureSize = width;

                var arg = "\"" + ddsfile + "\"";
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = nv_tool,
                    Arguments = arg,
                    WorkingDirectory = Application.StartupPath + "\\bin\\"
                };
                var process = Process.Start(startInfo);
                do
                {
                    //
                } while (!process.HasExited);
                process.Dispose();
                
                if (!ResizeImage(tgafile, TextureSize, format, output_path))
                {
                    DeleteFile(tgafile);
                    return false;
                }
                DeleteFile(ddsfile);
                DeleteFile(tgafile);
                if (delete_original)
                {
                    DeleteFile(rb_image);
                }
                Application.DoEvents();
                return true;
            }
            catch (Exception)
            {
                if (!rb_image.EndsWith(".dds", StringComparison.Ordinal))
                {
                    DeleteFile(ddsfile);
                }
                return false;
            }
        }

        /// <summary>
        /// Converts png_wii files to usable format
        /// </summary>
        /// <param name="wii_image">Full path to the png_xbox / png_ps3 / dds file</param>
        /// <param name="output_path">Full path you'd like to save the converted image</param>
        /// <param name="format">Allowed formats: BMP | JPG | PNG (default)</param>
        /// <param name="delete_original">True: delete | False: keep (default)</param>
        /// <returns></returns>
        public bool ConvertWiiImage(string wii_image, string output_path, string format, bool delete_original)
        {
            var tplfile = Path.GetDirectoryName(wii_image) + "\\" + Path.GetFileNameWithoutExtension(wii_image) + ".tpl";
            var pngfile = tplfile.Replace(".tpl", ".png");
            var Headers = new ImageHeaders();
            
            TextureSize = 128;
            DeleteFile(pngfile);

            try
            {
                if (tplfile != wii_image)
                {
                    DeleteFile(tplfile);

                    var binaryReader = new BinaryReader(File.OpenRead(wii_image));
                    var binaryWriter = new BinaryWriter(new FileStream(tplfile, FileMode.Create));

                    var wii_header = new byte[32];
                    binaryReader.Read(wii_header, 0, 32);

                    byte[] tpl_header;
                    if (wii_header.SequenceEqual(Headers.wii_128x256))
                    {
                        tpl_header = Headers.tpl_128x256;
                        TextureSize = 256;
                    }
                    else if (wii_header.SequenceEqual(Headers.wii_128x128_rgba32))
                    {
                        tpl_header = Headers.tpl_128x128_rgba32;
                    }
                    else if (wii_header.SequenceEqual(Headers.wii_256x256) ||
                             wii_header.SequenceEqual(Headers.wii_256x256_B) ||
                             wii_header.SequenceEqual(Headers.wii_256x256_c8))
                    {
                        tpl_header = Headers.tpl_256x256;
                        TextureSize = 256;
                    }
                    else if (wii_header.SequenceEqual(Headers.wii_128x128))
                    {
                        tpl_header = Headers.tpl_128x128;
                    }
                    else if (wii_header.SequenceEqual(Headers.wii_64x64))
                    {
                        TextureSize = 64;
                        tpl_header = Headers.tpl_64x64;
                    }
                    else
                    {
                        MessageBox.Show("File " + Path.GetFileName(wii_image) +
                            " has a header I don't recognize, so I can't convert it",
                            "Nemo Tools", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return false;
                    }
                    binaryWriter.Write(tpl_header);

                    var buffer = new byte[64];
                    int num;
                    do
                    {
                        num = binaryReader.Read(buffer, 0, 64);
                        if (num > 0)
                            binaryWriter.Write(buffer);
                    } while (num > 0);
                    binaryWriter.Dispose();
                    binaryReader.Dispose();
                }

                //this is so image quality is higher than the default
                var myEncoder = Encoder.Quality;
                var myEncoderParameters = new EncoderParameters(1);
                var myEncoderParameter = new EncoderParameter(myEncoder, 100L);
                myEncoderParameters.Param[0] = myEncoderParameter;

                var img = TPL.ConvertFromTPL(tplfile);
                img.Save(pngfile, GetEncoderInfo("image/png"), myEncoderParameters);
                img.Dispose();

                if (!File.Exists(pngfile))
                {
                    if (tplfile != wii_image)
                    {
                        DeleteFile(tplfile);
                    }
                    return false;
                }
                if (!format.ToLowerInvariant().Contains("png"))
                {
                    var image = NemoLoadImage(pngfile);
                    if (!ResizeImage(pngfile, image.Width, format, output_path))
                    {
                        image.Dispose();
                        DeleteFile(pngfile);
                        return false;
                    }
                    image.Dispose();
                }

                if (tplfile != wii_image && !KeepDDS)
                {
                    DeleteFile(tplfile);
                }
                if (!format.ToLowerInvariant().Contains("png"))
                {
                    DeleteFile(pngfile);
                }
                if (delete_original)
                {
                    DeleteFile(wii_image);
                }
                Application.DoEvents();
                return true;
            }
            catch (Exception)
            {
                if (tplfile != wii_image)
                {
                    DeleteFile(tplfile);
                }
                DeleteFile(pngfile);
                return false;
            }
        }

        /// <summary>
        /// Use to resize images up or down or convert across BMP/JPG/PNG/TIF
        /// </summary>
        /// <param name="image_path">Full file path to source image</param>
        /// <param name="image_size">Integer for image size, can be smaller or bigger than source image</param>
        /// <param name="format">Format to save the image in: BMP | JPG | TIF | PNG (default)</param>
        /// <param name="output_path">Full file path to output image</param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        public bool ResizeImage(string image_path, int image_size, string format, string output_path)
        {
            try
            {
                var newimage = Path.GetDirectoryName(output_path) + "\\" + Path.GetFileNameWithoutExtension(output_path);

                Il.ilInit();
                Ilu.iluInit();

                var imageId = new int[10];

                // Generate the main image name to use
                Il.ilGenImages(1, imageId);

                // Bind this image name
                Il.ilBindImage(imageId[0]);

                // Loads the image into the imageId
                if (!Il.ilLoadImage(image_path))
                {
                    return false;
                }
                // Enable overwriting destination file
                Il.ilEnable(Il.IL_FILE_OVERWRITE);

                var height = image_size;
                var width = image_size;

                //assume we're downscaling, this is better filter
                const int scaler = Ilu.ILU_BILINEAR;

                //resize image
                Ilu.iluImageParameter(Ilu.ILU_FILTER, scaler);
                Ilu.iluScale(width, height, 1);

                if (format.ToLowerInvariant().Contains("bmp"))
                {
                    //disable compression
                    Il.ilSetInteger(Il.IL_BMP_RLE, 0);
                    newimage = newimage + ".bmp";
                }
                else if (format.ToLowerInvariant().Contains("jpg") || format.ToLowerInvariant().Contains("jpeg"))
                {
                    Il.ilSetInteger(Il.IL_JPG_QUALITY, 99);
                    newimage = newimage + ".jpg";
                }
                else if (format.ToLowerInvariant().Contains("tif"))
                {
                    newimage = newimage + ".tif";
                }
                else if (format.ToLowerInvariant().Contains("tga"))
                {
                    Il.ilSetInteger(Il.IL_TGA_RLE, 0);
                    newimage = newimage + ".tga";
                }
                else
                {
                    Il.ilSetInteger(Il.IL_PNG_INTERLACE, 0);
                    newimage = newimage + ".png";
                }

                if (!Il.ilSaveImage(newimage))
                {
                    return false;
                }

                // Done with the imageId, so let's delete it
                Il.ilDeleteImages(1, imageId);
                Application.DoEvents();
                return File.Exists(newimage);
            }
            catch (AccessViolationException)
            {}
            catch (Exception)
            {}
            return false;
        }

        [HandleProcessCorruptedStateExceptions]
        public void CreateBlurredArt(string raw_input, string output)
        {
            if (!File.Exists(raw_input)) return;
            DeleteFile(output);

            //verify that the alleged png file is actually png and not jpg with changed extension
            //this was observed with GH to PS conversions
            var png = new byte[] {0x89, 0x50, 0x4E, 0x47};
            var header = new byte[png.Length];
            using (var br = new BinaryReader(File.Open(raw_input, FileMode.Open, FileAccess.Read)))
            {
                header = br.ReadBytes(header.Length);
            }
            var input = raw_input;
            var ext = Path.GetExtension(raw_input);
            if (!header.SequenceEqual(png) && ext.ToLowerInvariant() == ".png")
            {
                //it's not PNG, assume it's JPG
                input = raw_input.Replace(ext, ".jpg");
                DeleteFile(input);
                File.Copy(raw_input, input);
            }

            try
            {
                Il.ilInit();
                Ilu.iluInit();

                var img = Image.FromFile(input);
                var orig_width = img.Width;
                img.Dispose();

                var imageId = new int[2];
                Il.ilGenImages(2, imageId);
                Il.ilBindImage(imageId[0]);
                if (!Il.ilLoadImage(input))
                {
                    Il.ilDeleteImages(2, imageId);
                    return;
                }

                var scaler = Ilu.ILU_BILINEAR;
                if (orig_width > 512)
                {
                    Ilu.iluImageParameter(Ilu.ILU_FILTER, scaler);
                    Ilu.iluScale(512, 512, 1);
                    orig_width = 512;
                }

                scaler = Ilu.ILU_SCALE_TRIANGLE;
                Ilu.iluImageParameter(Ilu.ILU_FILTER, scaler);

                Il.ilBindImage(imageId[1]);
                if (!Il.ilLoadImage(input))
                {
                    Il.ilDeleteImages(2, imageId);
                    return;
                }

                Il.ilEnable(Il.IL_FILE_OVERWRITE);
                Il.ilSetInteger(Il.IL_PNG_INTERLACE, 0);

                const int width = 590;
                const int height = 654;

                //resize image
                Ilu.iluScale(width, height, 1);
                Ilu.iluBlurGaussian(25);
                Il.ilOverlayImage(imageId[0], (width - orig_width)/2, (height - orig_width)/2, 0);
                Il.ilSaveImage(output);
                Il.ilDeleteImages(2, imageId);
                if (input != raw_input)
                {
                    DeleteFile(input);
                }
            }
            catch (AccessViolationException)
            {}
            catch (Exception)
            {}
        }

        /// <summary>
        /// Returns string with correctly formatted characters
        /// </summary>
        /// <param name="raw_line">Raw line from songs.dta file</param>
        /// <returns></returns>
        public string FixBadChars(string raw_line)
        {
            var line = raw_line.Replace("Ã¡", "á");
            line = line.Replace("Ã©", "é");
            line = line.Replace("Ã¨", "è");
            line = line.Replace("ÃŠ", "Ê");
            line = line.Replace("Ã¬", "ì");
            line = line.Replace("Ã­­­", "í");
            line = line.Replace("Ã¯", "ï");
            line = line.Replace("Ã–", "Ö");
            line = line.Replace("Ã¶", "ö");
            line = line.Replace("Ã³", "ó");
            line = line.Replace("Ã²", "ò");
            line = line.Replace("Ãœ", "Ü");
            line = line.Replace("Ã¼", "ü");
            line = line.Replace("Ã¹", "ù");
            line = line.Replace("Ãº", "ú");
            line = line.Replace("Ã¿", "ÿ");
            line = line.Replace("Ã±", "ñ");
            line = line.Replace("ï¿½", "");
            line = line.Replace("�", "");
            line = line.Replace("E½", "");
            return line;
        }
        
        /// <summary>
        /// Returns byte array in hex value
        /// </summary>
        /// <param name="xIn">String value to be converted to hex</param>
        /// <returns></returns>
        public byte[] ToHex(string xIn)
        {
            for (var i = 0; i < (xIn.Length % 2); i++)
                xIn = "0" + xIn;
            var xReturn = new List<byte>();
            for (var i = 0; i < (xIn.Length / 2); i++)
                xReturn.Add(Convert.ToByte(xIn.Substring(i * 2, 2), 16));
            return xReturn.ToArray();
        }
        
        /// <summary>
        /// Returns clean Track Name from midi event string
        /// </summary>
        /// <param name="raw_event">The raw ToString value of the midi event</param>
        /// <returns></returns>
        public string GetMidiTrackName(string raw_event)
        {
            var name = raw_event;
            name = name.Substring(2, name.Length - 2); //remove track number
            name = name.Replace("SequenceTrackName", "");
            return name.Trim();
        }
        
        /// <summary>
        /// Returns cleaned string for file names, etc
        /// </summary>
        /// <param name="raw_string">Raw string from the songs.dta file</param>
        /// <param name="removeDash">Whether to remove dashes from the string</param>
        /// <param name="DashForSlash">Whether to replace slashes with dashes</param>
        /// <returns></returns>
        public string CleanString(string raw_string, bool removeDash, bool DashForSlash = false)
        {
            var mystring = raw_string;

            //remove forbidden characters if present
            mystring = mystring.Replace("\"", "");
            mystring = mystring.Replace(">", " ");
            mystring = mystring.Replace("<", " ");
            mystring = mystring.Replace(":", " ");
            mystring = mystring.Replace("|", " ");
            mystring = mystring.Replace("?", " ");
            mystring = mystring.Replace("*", " ");
            mystring = mystring.Replace("'", "");
            mystring = mystring.Replace("&#8217;", "'"); //Don't Speak
            mystring = mystring.Replace("   ", "");
            mystring = mystring.Replace("  ", "");
            mystring = mystring.Replace(" ", "");
            mystring = FixBadChars(mystring).Trim();

            if (removeDash)
            {
                if (mystring.Substring(0, 1) == "-") //if starts with -
                {
                    mystring = mystring.Substring(1, mystring.Length - 1);
                }
                if (mystring.Substring(mystring.Length - 1, 1) == "-") //if ends with -
                {
                    mystring = mystring.Substring(0, mystring.Length - 1);
                }

                mystring = mystring.Trim();
            }

            if (mystring.EndsWith(".", StringComparison.Ordinal)) //can't have files like Y.M.C.A.
            {
                mystring = mystring.Substring(0, mystring.Length - 1);
            }

            mystring = mystring.Replace("\\", DashForSlash && mystring != "AC/DC" ? "-" : (mystring != "AC/DC" ? " " : ""));
            mystring = mystring.Replace("/", DashForSlash && mystring != "AC/DC" ? "-" : (mystring != "AC/DC" ? " " : ""));

            return mystring;
        }

        public MidiFile NemoLoadMIDI(string midi_in)
        {
            //NAudio is limited in its ability to read some non-standard MIDIs
            //before this step was added, 3 different errors would prevent this program from reading
            //MIDIs with those situations
            //thanks raynebc we can fix them first and load the fixed MIDIs
            var midishrink = Application.StartupPath + "\\bin\\midishrink.exe";
            if (!File.Exists(midishrink)) return null;
            var midi_out = Application.StartupPath + "\\bin\\temp.mid";
            DeleteFile(midi_out);
            MidiFile MIDI;
            try
            {
                MIDI = new MidiFile(midi_in, false);
            }
            catch (Exception)
            {
                var folder = Path.GetDirectoryName(midi_in) ?? Environment.CurrentDirectory;
                var startInfo = new ProcessStartInfo
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    FileName = Application.StartupPath + "\\bin\\midishrink.exe",
                    Arguments = "\"" + midi_in + "\" \"" + midi_out + "\"",
                    WorkingDirectory = folder
                };
                var start = (DateTime.Now.Minute * 60) + DateTime.Now.Second;
                var process = Process.Start(startInfo);
                do
                {
                    //this code checks for possible memory leak in midishrink
                    //typical usage outputs a fixed file in 1 second or less, at 15 seconds there's a problem
                    if ((DateTime.Now.Minute * 60) + DateTime.Now.Second - start < 15) continue;
                    break;

                } while (!process.HasExited);
                if (!process.HasExited)
                {
                    process.Kill();
                    process.Dispose();
                }
                if (File.Exists(midi_out))
                {
                    try
                    {
                        MIDI = new MidiFile(midi_out, false);
                    }
                    catch (Exception)
                    {
                        MIDI = null;
                    }
                }
                else
                {
                    MIDI = null;
                }
            }
            DeleteFile(midi_out);  //the file created in the loop is useless, delete it
            return MIDI;
        }

        /// <summary>
        /// Use to quickly grab value on right side of = in C3 options/fix files
        /// </summary>
        /// <param name="raw_line">Raw line from the c3 file</param>
        /// <returns></returns>
        public string GetConfigString(string raw_line)
        {
            var line = raw_line;
            var index = line.IndexOf("=", StringComparison.Ordinal) + 1;
            try
            {
                line = line.Substring(index, line.Length - index);
            }
            catch (Exception)
            {
                line = "";
            }
            return line.Trim();
        }

        public ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            var encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        /// <summary>
        /// Loads image and unlocks file for uses elsewhere. USE THIS!
        /// </summary>
        /// <param name="file">Full path to the image file</param>
        /// <returns></returns>
        public Image NemoLoadImage(string file)
        {
            if (!File.Exists(file))
            {
                return null;
            }
            Image img = null;
            try
            {
                using (var bmpTemp = new Bitmap(file))
                {
                    img = new Bitmap(bmpTemp);
                }
            }
            catch (Exception)
            {}
            return img;
        }

        /// <summary>
        /// Returns line with featured artist normalized as 'ft.'
        /// </summary>
        /// <param name="line">Line to normalize</param>
        /// <returns></returns>
        public string FixFeaturedArtist(string line)
        {
            if (string.IsNullOrEmpty(line)) return "";

            var adjusted = line;

            adjusted = adjusted.Replace("Featuring", "ft.");
            adjusted = adjusted.Replace("featuring", "ft.");
            adjusted = adjusted.Replace("feat.", "ft.");
            adjusted = adjusted.Replace("Feat.", "ft.");
            adjusted = adjusted.Replace(" ft ", " ft. ");
            adjusted = adjusted.Replace(" FT ", " ft. ");
            adjusted = adjusted.Replace("Ft. ", "ft. ");
            adjusted = adjusted.Replace("FEAT. ", "ft. ");
            adjusted = adjusted.Replace(" FEAT ", " ft. ");

            if (adjusted.StartsWith("ft ", StringComparison.Ordinal))
            {
                adjusted = "ft. " + adjusted.Substring(3, adjusted.Length - 3);
            }

            return FixBadChars(adjusted);
        }

        /// <summary>
        /// Loads and formats help file for display on the HelpForm
        /// </summary>
        /// <param name="file">Name of the file, path assumed to be \bin\help/</param>
        /// <returns></returns>
        public string ReadHelpFile(string file)
        {
            var message = "";
            var helpfile = Application.StartupPath + "\\bin\\help\\" + file;
            if (File.Exists(helpfile))
            {
                var sr = new StreamReader(helpfile);
                while (sr.Peek() > 0)
                {
                    var line = sr.ReadLine();
                    message = message == "" ? line : message + "\r\n" + line;
                }
                sr.Dispose();
            }
            else
            {
                message = "Could not find help file, please redownload this program and DO NOT delete any files";
            }

            return message;
        }

        public Color LightenColor(Color color)
        {
            var correctionFactor = (float) 0.20;

            var red = (float)color.R;
            var green = (float)color.G;
            var blue = (float)color.B;

            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }

        private const string ClientId = "GET YOUR OWN FROM IMGUR"; //from imgur, specific to this program, do not use elsewhere
        public string UploadToImgur(string image)
        {
            //remove code snippet below once you get your own code from imgur and enter above
            MessageBox.Show("You tried to upload to Imgur but haven't added your own Imgur code. Sign up for app access with Imgur then go to source code under 'UploadtoImgur' in 'NemoTools.cs' and enter your ClientID there and remove this pop-up message.",
                "HEY YOU", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            return "";
            //remove code snippet above once you get your own code from imgur and enter above
            var link = "";
            try
            {
                using (var w = new WebClient())
                {
                    var values = new NameValueCollection
                        {
                            {"image", Convert.ToBase64String(File.ReadAllBytes(image))}
                        };
                    w.Headers.Add("Authorization", "Client-ID " + ClientId);
                    var response = w.UploadValues("https://api.imgur.com/3/upload.xml", values);
                    var sr = new StreamReader(new MemoryStream(response), Encoding.Default);
                    while (sr.Peek() >= 0)
                    {
                        var line = sr.ReadLine();
                        if (line == null || !line.Contains("link")) continue;
                        //get substring starting at http
                        line = line.Substring(line.IndexOf(":", StringComparison.Ordinal) - 4, line.Length - line.IndexOf(":", StringComparison.Ordinal));
                        //split string starting at </link
                        line = line.Substring(0, line.IndexOf("<", StringComparison.Ordinal));
                        link = line;
                    }
                    sr.Dispose();
                }
            }
            catch (Exception ex)
            {
                var error = ex.Message;
                link = "";
                if (error.Contains("429"))
                {
                    error = "Error Code 429: Rate limiting\nThis most likely means you've uploaded too many images\nPlease wait a couple of hours and try again";
                }
                else if (error.Contains("500"))
                {
                    error = "Error Code 500: Unexpected internal error\nThis means something is broken with the imgur service\nPlease try again later";
                }
                if (MessageBox.Show("Sorry, there was an error in uploading the file!\nThe error says:\n" + error + "\nTry again?", "Error",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == DialogResult.Yes)
                {
                    UploadToImgur(image);
                }
            }
            return link;
        }

        public bool HasMasterPassword()
        {
            //REDACTED BY TROJANNEMO
            return false;
        }

        public bool HasCorrectPassword(string authFile, string authPassword)
        {
            //REDACTED BY TROJANNEMO
            return false;
        }
        
        #region Mogg Stuff
        public void ReleaseStreamHandle(bool isNext)
        {
            try
            {
                if (isNext)
                {
                    NextOggStreamHandle.Free();
                }
                else
                {
                    PlayingOggStreamHandle.Free();
                }
            }
            catch (Exception)
            {}
        }

        public IntPtr GetOggStreamIntPtr(bool isNext)
        {
            ReleaseStreamHandle(isNext);
            if (isNext)
            {
                NextOggStreamHandle = GCHandle.Alloc(NextSongOggData, GCHandleType.Pinned);
                return NextOggStreamHandle.AddrOfPinnedObject();
            }
            PlayingOggStreamHandle = GCHandle.Alloc(PlayingSongOggData, GCHandleType.Pinned);
            return PlayingOggStreamHandle.AddrOfPinnedObject();
        }

        public bool MoggIsEncrypted(byte[] mData)
        {
            //REDACTED BY TROJANNEMO
            return false;
        }

        public bool DecM(byte[] mData, bool bypass, bool keep_header, bool isNext, DecryptMode mode, string mOut = "")
        {
            //REDACTED BY TROJANNEMO
            return false;
        }

        public void RemoveMHeader(byte[] mData, bool isNext, DecryptMode mode, string mOut)
        {
            byte[] buffer;
            using (var br = new BinaryReader(new MemoryStream(mData)))
            {
                br.ReadInt32();
                var num = br.ReadInt32();
                br.BaseStream.Seek(num, SeekOrigin.Begin);
                buffer = new byte[br.BaseStream.Length - num];
                br.Read(buffer, 0, buffer.Length);
            }
            if (mode == DecryptMode.ToMemory)
            {
                if (isNext)
                {
                    NextSongOggData = buffer;
                }
                else
                {
                    PlayingSongOggData = buffer;
                }
            }
            else
            {
                WriteOutData(buffer, mOut);
            }
        }

        public void WriteOutData(byte[] mData, string mOut)
        {
            DeleteFile(mOut);
            using (var fs = File.Create(mOut))
            {
                using (var bw = new BinaryWriter(fs))
                {
                    bw.Write(mData);
                }
            }
        }

        public bool IsC3Mogg(byte[] mData)
        {
            //REDACTED BY TROJANNEMO
            return false;
        }
        
        public byte[] DeObfM(byte[] mData)
        {
            //REDACTED BY TROJANNEMO
            return mData;
        }

        public byte[] ObfM(byte[] mData)
        {
            //REDACTED BY TROJANNEMO
            return mData;
        }
        
        private GCHandle PlayingOggStreamHandle;
        private GCHandle NextOggStreamHandle;
        public byte[] PlayingSongOggData;
        public byte[] NextSongOggData;
        #endregion
    }

    public enum DecryptMode
    {
        ToFile,
        ToMemory
    }

    public enum CryptVersion
    {
        x0A = 0x0A, //No encryption
        x0B = 0x0B, //RB1, RB1 DLC
        x0C = 0x0C, //RB2, AC/DC Live, some RB2 DLC
        x0E = 0x0E, //Lego, Green Day, most RB2 DLC
        x0F = 0x0F, //RBN
        x10 = 0x10, //RB3, RB3 DLC
    }
}
