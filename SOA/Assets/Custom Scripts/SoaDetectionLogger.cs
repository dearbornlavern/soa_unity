using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

public class SoaDetectionLogger {

    private BinaryWriter writer;

    public SoaDetectionLogger(string outputFilename)
    {
        writer = new BinaryWriter(File.Open(outputFilename, FileMode.Create));
        
    }

    public void logDetection(UInt64 time, int detectorId, int detectedId)
    {
        writer.Write(time);
        writer.Write(detectorId);
        writer.Write(detectedId);
    }


    private static ReadDetectionLog logReader = null;

    public static void openLogFile(String logFileName)
    {
        logReader = new ReadDetectionLog(logFileName);
    }

    public static bool readDetectionLog(out UInt64 time, out int detectorId, out int detectedId)
    {
        if (logReader != null)
        {
            return logReader.readDetection(out time, out detectorId, out detectedId);
        }

        time = 0;
        detectedId = 0;
        detectorId = 0;
        return false;
    }

    private class ReadDetectionLog
    {
        BinaryReader reader;
        public ReadDetectionLog(string logFileName)
        {
            reader = new BinaryReader(File.Open(logFileName, FileMode.Open));
        }

        public bool readDetection(out UInt64 time, out int detectorId, out int detectedId)
        {
            
            try{
                time = reader.ReadUInt64();
                detectorId = reader.ReadInt32();
                detectedId = reader.ReadInt32();
            }catch(Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                time = 0;
                detectorId = 0;
                detectedId = 0;
                return false;
            }

            return true;
        }
    }
}
