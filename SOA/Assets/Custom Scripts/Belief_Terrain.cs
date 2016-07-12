using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    enum Terrain {NONE = 0, MOUNTAIN = 1, WATER = 2 };
    public class Belief_Terrain : Belief
    {
        // Members
        private int type;
        private List<GridCell> cells;

        // Constructor
        public Belief_Terrain(int type, List<GridCell> cells)
            : base(type)
        {
            this.type = type;
            this.cells = GridCell.cloneList(cells);
        }

        // Type information
        public override BeliefType getBeliefType()
        {
            return BeliefType.TERRAIN;
        }

        // String representation
        public override string ToString()
        {
            string s = "Belief_Terrain {"
                + "\n"
                + "\ttype=" + type + "\n"
                + "\ttime=" + getBeliefTime() + "\n"
                + "\tid=" + getId() + "\n";

            for (int i = 0; i < cells.Count; i++)
            {
                s += "\t" + cells[i] + "\n";
            }
            s += "}";
            return s;
        }

        // Get methods
        public int getType() { return type; }
        public List<GridCell> getCells() { return GridCell.cloneList(cells); }
    }
}
