using ffmpeg_convert;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testConvert
{
    class Program
    {
        static FileInfo input = new FileInfo(@"F:\Music\[111111]TABLACKE(FLAC)\[Album] T-ara - Black Eyes (FLAC-Lossless) [bambimiri.blogspot]\01 CRY CRY.flac");
        static FileInfo output = new FileInfo(@"out.mp3");
            
        static void Main(string[] args)
        {
            Console.WriteLine(output.FullName);
            Console.WriteLine();
            FFmpeg.Mp3ConversionArgs argus = new FFmpeg.Mp3ConversionArgs();
            FFmpeg mFFmepg = new FFmpeg();
            mFFmepg.Converter.ProgressChanged += PrintProgress;
            mFFmepg.Converter.ToMp3(input, output, argus);
            Console.ReadLine();
            
        }

        private static void PrintProgress(object sender, FFmpeg.Convert.ProgressChangedEventArgs e)
        {
            int progresoPorcentaje = Convert.ToInt32(e.Progress * 100);
            Console.WriteLine("Converting "+input.Name+" to "+output.Name+". "+progresoPorcentaje+"%");
        }
    }
}
