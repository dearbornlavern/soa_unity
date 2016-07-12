using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class Belief_Base : Belief
    {
        // Members
        private int id;
        private List<GridCell> cells;
        private float supplies;

        // Constructor
        public Belief_Base(int id, List<GridCell> cells, float supplies)
            : base(id)
        {
            this.id = id;
            this.cells = GridCell.cloneList(cells);
            this.supplies = supplies;
        }

        // Type information
        public override BeliefType getBeliefType()
        {
            return BeliefType.BASE;
        }

        // String representation
        public override string ToString()
        {
            string s = "Belief_Base {"
                + "\n" + "  id: " + id;
            for (int i = 0; i < cells.Count; i++)
            {
                s += "\n  " + cells[i];
            }
            s += "\n" + "  supplies: " + supplies;
            s += "\n" + "}";
            return s;
        }

        // Get methods
	    public override int getId(){ return id; }
        public List<GridCell> getCells() { return GridCell.cloneList(cells); }
        public float getSupplies() { return supplies; }
    }
}
