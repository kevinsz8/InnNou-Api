namespace InnNou.Application.Common
{
    // Shared by CreateParLevelOverrideCommandHandler (handler-entry-point pre-check) and
    // ParLevelService.CreateOverrideAsync (the defensive re-check every service method keeps
    // regardless of what the handler already validated) — a single source of truth so the two
    // layers can never silently drift on what counts as a valid seasonal boundary.
    public static class ParLevelDateValidation
    {
        // Validates the (month, day) pair is a real calendar date, using a non-leap reference
        // year (2001) so Feb 29 is rejected as a boundary value — a deliberate simplification
        // that sidesteps all leap-year ambiguity in a SEASONAL override's wrap-around math, at
        // the cost of forcing an end-of-year window to use Feb 28/Mar 1 instead of Feb 29.
        public static bool IsValidMonthDay(int month, int day)
        {
            try
            {
                _ = new DateTime(2001, month, day);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}
