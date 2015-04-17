﻿using System;
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

        // Constructor
        public Belief_NGOSite(int id, List<GridCell> cells)
            : base(id)
        {
            this.id = id;
            this.cells = GridCell.cloneList(cells);
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
            s += "\n" + "}";
            return s;
        }

        // Get methods
	    public int getId(){ return id; }
        public List<GridCell> getCells() { return GridCell.cloneList(cells); }
    }
}
