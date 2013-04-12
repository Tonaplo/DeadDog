﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeadDog.Console
{
    /// <summary>
    /// Represents a menu where each option is associated with an <see cref="Func{T}"/> delegate.
    /// </summary>
    /// <typeparam name="T">The type of elements returned by the menu.</typeparam>
    public class Menu<T> : MenuBase<Func<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Menu{T}"/> class with a specified title.
        /// </summary>
        /// <param name="title">The title of the menu.</param>
        public Menu(string title)
            : base(title)
        {
        }

        /// <summary>
        /// Adds a new option to the menu, which returns a constant value.
        /// </summary>
        /// <param name="text">The text displayed for the new option.</param>
        /// <param name="value">The value returned by the new option.</param>
        /// <param name="color">The color of the text displayed for the new option.</param>
#if NET3
        public void Add(string text, T value, ConsoleColor color)
#else
        public void Add(string text, T value, ConsoleColor color = ConsoleColor.Gray)
#endif
        {
            this.Add(() => value, text, color);
        }

        /// <summary>
        /// Sets the cancel option for the menu.
        /// The default value of <typeparamref name="T"/> is returned if the cancel option is selected.
        /// </summary>
        /// <param name="text">The text displayed for the cancel option.</param>
        /// <param name="color">The color of the text displayed for the cancel option.</param>
#if NET3
        public void SetCancel(string text, ConsoleColor color)
#else
        public void SetCancel(string text, ConsoleColor color = ConsoleColor.Gray)
#endif
        {
            base.SetCancel(text, () => default(T), color);
        }
        /// <summary>
        /// Sets the cancel option for the menu.
        /// </summary>
        /// <param name="text">The text displayed for the cancel option.</param>
        /// <param name="value">The value of type <typeparamref name="T"/> that should be returned if the cancel option is selected.</param>
        /// <param name="color">The color of the text displayed for the cancel option.</param>
#if NET3
        public void SetCancel(string text, T value, ConsoleColor color)
#else
        public void SetCancel(string text, T value, ConsoleColor color = ConsoleColor.Gray)
#endif
        {
            base.SetCancel(text, () => value, color);
        }

        /// <summary>
        /// Shows the menu and waits for an option to be selected.
        /// When an option has been selected, its corresponding delegate is executed.
        /// </summary>
        /// <returns>The value returned by the delegate called (dependant on the option selected).</returns>
        public T Show()
        {
            int selected = ShowAndSelectIndex();

            var result = this[selected];
            return result.Item1();
        }

        /// <summary>
        /// Shows the menu and waits for an option to be selected.
        /// When an option has been selected, its corresponding delegate is executed.
        /// </summary>
        /// <param name="repeat">A boolean indicating whether the menu should be displayed repeatedly until the cancel option is selected.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the selected elements (one for each time the menu is displayed).</returns>
        public IEnumerable<T> Show(bool repeat)
        {
            if (!this.CanCancel && repeat)
                throw new InvalidOperationException("A menu cannot auto-repeat without a cancel option.");

            do
            {
                int selected = ShowAndSelectIndex();

                var result = this[selected];
                yield return result.Item1();
            } while (repeat && !this.WasCancelled);
        }
    }
}
