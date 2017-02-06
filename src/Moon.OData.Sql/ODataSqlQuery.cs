﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Moon.OData.Sql
{
    /// <summary>
    /// Represents SQL query with OData query options applied.
    /// </summary>
    public class ODataSqlQuery
    {
        private readonly List<object> arguments;
        private readonly string commandText;
        private readonly IODataOptions options;
        private readonly Lazy<string> result;

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataSqlQuery" /> class.
        /// </summary>
        /// <param name="commandText">The SQL command text to use as a starting point.</param>
        /// <param name="arguments">
        /// The argument values of the SQL command. Include an <see cref="IODataOptions" /> as the
        /// last item.
        /// </param>
        public ODataSqlQuery(string commandText, params object[] arguments)
        {
            Requires.NotNull(commandText, nameof(commandText));
            Requires.NotNull(arguments, nameof(arguments));

            var last = arguments.Length - 1;

            if (last >= 0 && arguments[last] is IODataOptions options)
            {
                this.options = options;
            }
            else
            {
                throw new InvalidOperationException("You've forgot to include ODataOptions as the last argument.");
            }

            this.commandText = commandText;
            this.arguments = new List<object>(arguments);
            this.arguments.Remove(options);

            if ((options.Count != null) && options.Count.Value)
            {
                Count = new CountSqlQuery(this, commandText, arguments);
            }

            result = Lazy.From(Build);
        }

        /// <summary>
        /// Gets or sets a function used to resolve primary key column name.
        /// </summary>
        public Func<Type, string> ResolveKey { get; set; }

        /// <summary>
        /// Gets or sets a function used to resolve column names.
        /// </summary>
        public Func<PropertyInfo, string> ResolveColumn { get; set; }

        /// <summary>
        /// Gets or sets whether the query options parser is case sensitive when matching names of
        /// properties. The default value is true.
        /// </summary>
        public bool IsCaseSensitive
        {
            get { return options.IsCaseSensitive; }
            set { options.IsCaseSensitive = value; }
        }

        /// <summary>
        /// Gets the SQL command that can be used to select total number of results ($orderby, $top
        /// and $skip options are not applied). The value is null if $count option is false or is
        /// not defined.
        /// </summary>
        public CountSqlQuery Count { get; }

        /// <summary>
        /// Gets the SQL command text with OData options applied.
        /// </summary>
        public string CommandText
            => result.Value;

        /// <summary>
        /// Gets the argument values of the SQL command.
        /// </summary>
        public object[] Arguments
            => CommandText != null ? arguments.ToArray() : new object[0];

        private string Build()
        {
            var builder = new StringBuilder();
            builder.Append(SelectClause.Build(commandText, options, ResolveColumn));
            builder.AppendWithSpace(WhereClause.Build(GetOperator(), arguments, options, ResolveColumn));
            builder.AppendWithSpace(OrderByClause.Build(options, ResolveColumn));
            builder.AppendWithSpace(OffsetClause.Build(options));
            return builder.ToString();
        }

        private string GetOperator()
            => commandText.Contains("WHERE", StringComparison.OrdinalIgnoreCase) ? "AND" : "WHERE";
    }
}