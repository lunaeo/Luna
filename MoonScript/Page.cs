using System.Collections.Generic;
using System.Linq;

namespace MoonScript
{
    /// <summary>
    /// Whenever a <see cref="TriggerCategory.Cause"/> trigger is discovered during parsing.
    /// </summary>
    /// <param name="sender"> The page the trigger was found in. </param>
    /// <param name="trigger"> The trigger that was found. </param>
    public delegate void CauseTriggerDiscoveryHandler(Page sender, List<Trigger> paragraph, Trigger trigger);

    /// <summary>
    /// Whenever a trigger is executed, for debugging purposes.
    /// </summary>
    /// <param name="sender"> The page the trigger was found in. </param>
    /// <param name="trigger"> The trigger that was executed. </param>
    public delegate void PageExecuteTriggerHandler(Page sender, List<Trigger> paragraph);

    /// <returns>
    ///  If true, continue to the next <see cref="Trigger"/> in the paragraph.
    ///  If false, terminate the execution of the current paragraph.
    /// </returns>
    /// <param name="trigger"> The trigger currently executed in the trigger block. </param>
    /// <param name="context"> The context object provided during the execution. </param>
    /// <param name="args"> Any additional information to pass along. </param>
    public delegate bool TriggerHandler(Trigger trigger, IContext context, object args);

    public delegate object VariableHandler(Trigger trigger, VariableType type, string key);

    public class Page
    {
        public MoonScriptEngine Engine { get; private set; }
        private List<List<Trigger>> TriggerBlocks { get; set; }

        public Dictionary<Trigger, TriggerHandler> Handlers { get; private set; }

        /// <summary>
        /// A list of variables set globally accessible to any <see cref="Trigger"/>.
        /// </summary>
        public List<Variable> Variables { get; private set; }

        /// <summary>
        /// The default area if there are none specified during an <see cref="TriggerCategory.Effect"/>.
        /// </summary>
        public Area DefaultArea { get; set; } = new Area();

        public PageExecuteTriggerHandler OnPageExecuteTrigger { get; }
        public CauseTriggerDiscoveryHandler OnPageDiscoverCauseTrigger { get; }
        public VariableHandler OnVariable { get; set; }

        /// <summary>
        /// A user-specified name of the page. Otherwise, null.
        /// </summary>
        public string Name { get; set; }

        public Page(MoonScriptEngine engine, PageExecuteTriggerHandler onPageExecuteTrigger, CauseTriggerDiscoveryHandler onPageDiscoverCauseTrigger)
        {
            this.Engine = engine;

            this.TriggerBlocks = new List<List<Trigger>>();
            this.Handlers = new Dictionary<Trigger, TriggerHandler>();
            this.Variables = new List<Variable>();
            this.OnPageExecuteTrigger = onPageExecuteTrigger;
            this.OnPageDiscoverCauseTrigger = onPageDiscoverCauseTrigger;
        }

        /// <summary>
        /// Set the specified variable, overriding if the key already exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetVariable(string key, IntVariable value)
        {
            if (this.Variables.Any(x => x.Type == VariableType.Number && x.Key == key))
            {
                this.Variables.Find(x => x.Type == VariableType.Number && x.Key == key).Value = value;
            }
            else
            {
                this.Variables.Add(new Variable(VariableType.Number, key, value));
            }
        }

        /// <summary>
        /// Set the specified message variable, overriding if the key already exists.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void SetMessageVariable(string key, object value)
        {
            if (this.Variables.Any(x => x.Type == VariableType.Message && x.Key == key))
            {
                this.Variables.Find(x => x.Type == VariableType.Message && x.Key == key).Value = value;
            }
            else
            {
                this.Variables.Add(new Variable(VariableType.Message, key, value));
            }
        }

        /// <summary>
        /// Assigns the specified TriggerHandler to <paramref name="trigger"/>.
        /// </summary>
        /// <remarks> By default, a non-set <see cref="TriggerCategory.Cause"/> trigger returns true. </remarks>
        /// <param name="trigger"><see cref="Trigger"/></param>
        /// <param name="handler"><see cref="TriggerHandler"/></para
        public void SetTriggerHandler(Trigger trigger, TriggerHandler handler, string description = null)
        {
            trigger.Description = description;

            if (this.Handlers.ContainsKey(trigger))
            {
                if (this.Engine.Options.CanOverrideTriggerHandlers)
                    this.Handlers[trigger] = handler;
                else throw new MoonScriptException($"A trigger handler for ({trigger.Category}:{trigger.Id} already exists.");
            }

            this.Handlers.Add(trigger, handler);
        }

        public void RemoveTriggerHandler(TriggerCategory category, int triggerId)
        {
            var triggers = from trigger in this.Handlers
                           where trigger.Key.Category == category
                           where trigger.Key.Id == triggerId
                           select trigger.Key;

            foreach (var trigger in triggers)
                this.Handlers.Remove(trigger);
        }

        internal Page InsertBlocks(List<List<Trigger>> triggerBlocks)
        {
            foreach (var paragraph in triggerBlocks)
            {
                this.OnPageDiscoverCauseTrigger?.Invoke(this, paragraph, paragraph[0]);
            }

            this.TriggerBlocks.AddRange(triggerBlocks);
            return this;
        }

        private void ExecuteBlock<T>(T triggerBlock, IContext context, object additionalArgs) where T : IList<Trigger>
        {
            var causeTrigger = triggerBlock[0];

            // set the current page for variable handling
            causeTrigger.Page = this;

            // debug execution block
            this.OnPageExecuteTrigger?.Invoke(this, triggerBlock.Cast<Trigger>().ToList());

            // if there is a handler set for the cause category, invoke before proceeding
            // cause triggers, if not handled, return true by default.
            if (this.Handlers.ContainsKey(causeTrigger))
            {
                if (!this.Handlers[causeTrigger](causeTrigger, context, additionalArgs))
                    return;
            }

            foreach (var t in triggerBlock)
                t.Context = context;

            // check if there is any additional conditions to evaluate
            for (var i = 1; i < triggerBlock.Count; i++)
            {
                var trigger = triggerBlock[i];

                // set the current page for variable handling
                trigger.Page = this;

                switch (trigger.Category)
                {
                    case TriggerCategory.Cause:
                        throw new MoonScriptException("You cannot have sibling causes.");

                    case TriggerCategory.Condition:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new MoonScriptException("You do not have a handler for this trigger.", trigger);

                        if (!this.Handlers[trigger](trigger, context, additionalArgs))
                            return;

                        triggerBlock.Last().Conditions.Add(trigger);
                        break;

                    case TriggerCategory.Area:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new MoonScriptException("You do not have a handler for this trigger.", trigger);

                        if (!this.Handlers[trigger](trigger, context, additionalArgs))
                            return;

                        triggerBlock.Last().Areas.Add(trigger);
                        break;

                    case TriggerCategory.Filter:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new MoonScriptException("You do not have a handler for this trigger.", trigger);

                        trigger.Areas = triggerBlock.LastOrDefault().Areas;
                        if (!this.Handlers[trigger](trigger, context, additionalArgs))
                            return;

                        triggerBlock.Last().Filters.Add(trigger);
                        break;

                    case TriggerCategory.Effect:
                        if (!this.Handlers.ContainsKey(trigger))
                            throw new MoonScriptException("You do not have a handler for this trigger.", trigger);

                        trigger.Context = context;
                        trigger.Area = triggerBlock.LastOrDefault(x => x.Category == TriggerCategory.Area)?.Area ?? this.DefaultArea;

                        // every condition has been met, execute the effect
                        this.Handlers[trigger](trigger, context, additionalArgs);
                        break;
                }
            }
        }

        /// <summary>
        /// Executes trigger paragraphs containing <see cref="TriggerCategory.Cause"/> with the specified <see cref="Trigger.Id"/>(s)
        /// </summary>
        /// <param name="context"> An object representing an entity which caused the trigger, optional. </param>
        /// <param name="additionalArgs"> An object representing any additional information to carry to the next trigger. </param>
        /// <param name="triggerIds"> The specified <see cref="TriggerCategory.Cause"/>(s) to execute. </param>
        public void Execute(IContext context = null, object additionalArgs = null, params int[] triggerIds)
        {
            foreach (var triggerId in triggerIds)
                foreach (var triggerBlock in this.TriggerBlocks)
                    if (triggerBlock[0].Id == triggerId)
                        this.ExecuteBlock(triggerBlock, context, additionalArgs);
        }

        /// <summary>
        /// Executes the specified trigger paragraph.
        /// </summary>
        /// <param name="context"> An object representing an entity which caused the trigger, optional. </param>
        /// <param name="additionalArgs"> An object representing any additional information to carry to the next trigger. </param>
        /// <param name="paragraph"> The specified <see cref="TriggerCategory.Cause"/>(s) to execute. </param>
        public void ExecuteParagraph(List<Trigger> paragraph, IContext context = null, object additionalArgs = null)
        {
            this.ExecuteBlock(paragraph, context, additionalArgs);
        }
    }
}