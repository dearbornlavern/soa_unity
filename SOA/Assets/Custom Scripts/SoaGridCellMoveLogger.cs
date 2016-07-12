using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;

public class SoaGridCellMoveLogger {

    private BinaryWriter writer;

    public SoaGridCellMoveLogger(string outputFilename)
    {
        writer = new BinaryWriter(File.Open(outputFilename, FileMode.Create));
    }

    public void logDetection(UInt64 time, int gridU, int gridV)
    {
        writer.Write(time);
        writer.Write(gridU);
        writer.Write(gridV);
    }

    private static ReadGridCellMoveLog logReader = null;

    public static void openLogFile(String logFileName)
    {
        logReader = new ReadGridCellMoveLog(logFileName);
    }

    public static bool readGridCellMoveLog(out UInt64 time, out int gridU, out int gridV)
    {
        if (logReader != null)
        {
            return logReader.readGridCellMove(out time, out gridU, out gridV);
        }

        time = 0;
        gridU = 0;
        gridV = 0;
        return false;
    }

    private class ReadGridCellMoveLog
    {
        BinaryReader reader;
        public ReadGridCellMoveLog(string logFileName)
        {
            reader = new BinaryReader(File.Open(logFileName, FileMode.Open));
        }

        public bool readGridCellMove(out UInt64 time, out int gridU, out int gridV)
        {

            try
            {
                time = reader.ReadUInt64();
                gridU = reader.ReadInt32();
                gridV = reader.ReadInt32();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.ToString());
                time = 0;
                gridU = 0;
                gridV = 0;
                return false;
            }

            return true;
        }
    }

}
