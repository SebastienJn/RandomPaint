using System;
using System.Diagnostics;
using Microsoft.Xna.Framework.Input;

namespace RandomPaint
{
    public class SelectionMode
    {
        private int selectionStepCount;
        private int remainingSelectionStep = -1;
        private DateTime selectionTimestamp = DateTime.MinValue;
        private readonly ColorMap[] selectionSet = new ColorMap[4];

        public bool Update_SelectionMode(bool selectCurrent, ref ColorMap _currentColorMap)
        {
            bool updateColorMapFromSelection = false;

            if (!selectCurrent)
            {
                bool computeNextTimestamp = false;

                if (remainingSelectionStep < 0)
                #region Intialize selection list
                {
                    for (var index = 0; index < selectionSet.Length; index++)
                    {
                        var x = (index & 0b01) * (_currentColorMap.Width / 2);
                        var y = ((index & 0b10) >> 1) * (_currentColorMap.Height / 2);

                        selectionSet[index] = _currentColorMap.GetHalfSizeSample(x, y);
                    }

                    selectionStepCount = 12 + Helpers.Random.Next(4);
                    remainingSelectionStep = selectionStepCount;
                    computeNextTimestamp = true;
                    updateColorMapFromSelection = true;
                }
                #endregion
                else
                #region Switch to next item in selection list (with rotation)
                {
                    if (DateTime.Now > selectionTimestamp) // Elapsed
                    {
                        if (remainingSelectionStep == 0)
                        {
                            // Finished
                            selectCurrent = true;
                        }
                        else
                        {
                            computeNextTimestamp = true;
                            remainingSelectionStep--;
                            updateColorMapFromSelection = true;
                        }
                    }
                }
                #endregion

                if (computeNextTimestamp)
                {
                    var timeSpan = TimeSpan.FromSeconds(
                        3 * selectionStepCount / (double)(selectionStepCount + remainingSelectionStep));
                    selectionTimestamp = DateTime.Now +
                                         timeSpan;
                }
            }

            if (selectCurrent)
            {
                remainingSelectionStep = -1;

                return false; // Exit from "selection" mode
            }

            if (updateColorMapFromSelection) // Move to next selection
            {
                _currentColorMap = selectionSet[remainingSelectionStep % 4];
            }

            return true;
            // currentState = State.Reprocess;
            //_generator.CurrentState = Generator.State.WarmUp;
        }
    }
}
