﻿/*
 * Copyright 2010-2014 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using NanoByte.Common.Tasks;

namespace ZeroInstall.Services
{
    /// <summary>
    /// A minimalistic <see cref="ITaskHandler"/> that allows you to pre-record answers and retrieve output.
    /// </summary>
    public class MockTaskHandler : SilentTaskHandler
    {
        /// <summary>
        /// Do not show progress reports, questions or messages (except for non-intrusive background messages like tray icons) unless a critical error occurs.
        /// </summary>
        public override bool Batch { get; set; }

        /// <summary>
        /// The prerecorded result for <see cref="AskQuestion"/>.
        /// </summary>
        public bool AnswerQuestionWith { get; set; }

        /// <summary>
        /// Last question passed to <see cref="AskQuestion"/>.
        /// </summary>
        public string LastQuestion { get; private set; }

        /// <summary>
        /// Fakes asking the user a question.
        /// </summary>
        /// <returns>The current value of <see cref="AnswerQuestionWith"/>.</returns>
        public override bool AskQuestion(string question, string batchInformation = null)
        {
            LastQuestion = question;
            return AnswerQuestionWith;
        }

        /// <summary>
        /// Last information string passed to <see cref="Output"/>.
        /// </summary>
        public string LastOutput { get; private set; }

        /// <summary>
        /// Fakes showing an information string output to the user.
        /// </summary>
        public override void Output(string title, string information)
        {
            LastOutput = information;
        }
    }
}
