﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace soa
{
    public class Belief_Village : Belief
    {
        // Members
        private int id;
        private List<GridCell> cells;
        private float supplies;
        private float casualties;

        // Constructor
        public Belief_Village(int id, List<GridCell> cells, float supplies, float casualties)
            : base(id)
        {
            this.id = id;
            this.cells = GridCell.cloneList(cells);
            this.supplies = supplies;
            this.casualties = casualties;
        }

        // Type information
        public override BeliefType getBeliefType()
        {
            return BeliefType.VILLAGE;
        }

        // String representation
        public override string ToString()
        {
            string s = "Belief_Village {"
                + "\n" + "  id: " + id;
            for (int i = 0; i < cells.Count; i++)
            {
                s += "\n  " + cells[i];
            }
            s += "\n" + "  supplies: " + supplies;
            s += "\n" + "  casualties: " + casualties;
            s += "\n" + "}";
            return s;
        }

        // Get methods
        public int getId() { return id; }
        public List<GridCell> getCells() { return GridCell.cloneList(cells); }
        public float getSupplies() { return supplies; }
        public float getCasualties() { return casualties; }
    }
}
