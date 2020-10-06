namespace MoonScript
{
    public enum TriggerCategory : int
    {
        /// <summary>
        /// A trigger defined with a 0
        /// <para>Example: (0:1) when someone says something, </para>
        /// </summary>
        Cause = 0,

        /// <summary>
        /// A trigger defined with a 1
        /// <para>Example: (1:2) and they moved # units left, </para>
        /// </summary>
        Condition = 1,

        /// <summary>
        /// A trigger defined with a 3
        /// <para>Example: (3:1) at position (#,#) on the map,</para>
        /// </summary>
        Area = 3,

        /// <summary>
        /// A trigger defined with a 4
        /// <para>Example: (4:1) only where the foreground block is type #,</para>
        /// </summary>
        Filter = 4,

        /// <summary>
        /// A trigger defined with a 5
        /// <para>Example: (5:1) place a letter with {...} written on it. </para>
        /// </summary>
        Effect = 5,

        /// <summary>
        /// A trigger that was not defined.  You should never encounter this
        /// if you do then something isn't quite right.
        /// </summary>
        Undefined = -1
    }
}