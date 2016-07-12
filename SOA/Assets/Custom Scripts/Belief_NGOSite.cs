using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class Belief_NGOSite : Belief
    {
        // Members
        private int id;
        private List<GridCell> cells;
        private float supplies;
        private float casualties;
        private float civilians;

        // Constructor
        public Belief_NGOSite(int id, List<GridCell> cells, float supplies, float casualties, float civilians)
            : base(id)
        {
            this.id = id;
            this.cells = GridCell.cloneList(cells);
            this.supplies = supplies;
            this.casualties = casualties;
            this.civilians = civilians;
        }

        // Type information
        public override BeliefType getBeliefType()
        {
            return BeliefType.NGOSITE;
        }

        // String representation
        public override string ToString()
        {
            string s = "Belief_NGOSite {"
                + "\n" + "  id: " + id;
            for (int i = 0; i < cells.Count; i++)
            {
                s += "\n  " + cells[i];
            }
            s += "\n" + "  supplies: " + supplies;
            s += "\n" + "  casualties: " + casualties;
            s += "\n" + "  civilians: " + civilians;
            s += "\n" + "}";
            return s;
        }

        // Get methods
	    public override int getId(){ return id; }
        public List<GridCell> getCells() { return GridCell.cloneList(cells); }
        public float getSupplies() { return supplies; }
        public float getCasualties() { return casualties; }
        public float getCivilians() { return civilians; }
    }
}
