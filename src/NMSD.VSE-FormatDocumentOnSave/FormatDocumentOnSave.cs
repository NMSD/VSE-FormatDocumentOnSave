﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.TextManager.Interop;

namespace NMSD.VSE_FormatDocumentOnSave
{
    internal class FormatDocumentOnSave
    {
        private CommandEvents commandEvents;

        private DTE dte;

        private static bool shouldRegisterFormatDocumentOnSave = true;
        private readonly IVsTextManager txtMngr;

        public FormatDocumentOnSave(DTE dte, IVsTextManager txtMngr)
        {
            this.txtMngr = txtMngr;
            this.dte = dte;
            if (shouldRegisterFormatDocumentOnSave)
            {
                commandEvents = dte.Events.CommandEvents;
                commandEvents.BeforeExecute += commandEvents_BeforeExecute;

                shouldRegisterFormatDocumentOnSave = false;
            }
        }

        void commandEvents_BeforeExecute(string Guid, int ID, object CustomIn, object CustomOut, ref bool CancelDefault)
        {
            FormatDocument(Guid, (uint)ID);
        }

        void FormatCurrentActiveDocument()
        {
            try
            {
                bool useManualSelection = dte.ActiveDocument.ProjectItem.Name.EndsWith(".cshtml", true, CultureInfo.InvariantCulture);

                if (dte.ActiveWindow.Kind == "Document")
                {
                    if (useManualSelection)
                        FormatCSHTML();
                    else
                        dte.ExecuteCommand("Edit.FormatDocument", string.Empty);
                }
            }
            catch (Exception) { }
        }

        void FormatCSHTML()
        {
            IVsTextView textViewCurrent;
            txtMngr.GetActiveView(1, null, out textViewCurrent);    // Gets the TextView (TextEditor) for the current active document
            int a, b, c, verticalScrollPosition;
            var scrollInfo = textViewCurrent.GetScrollInfo(1, out a, out b, out c, out verticalScrollPosition);

            dynamic selection = dte.ActiveDocument.Selection;
            int line = selection.CurrentLine;
            int lineLength = selection.ActivePoint.LineLength;
            int col = selection.CurrentColumn;

            dte.ExecuteCommand("Edit.FormatDocument", string.Empty);

            if (!selection.IsEmpty)
                selection.Cancel();

            selection.GoToLine(line);
            int offset = col - (lineLength - selection.ActivePoint.LineLength);
            selection.MoveToLineAndOffset(line, offset, false);

            textViewCurrent.SetScrollPosition(1, verticalScrollPosition);
        }

        void FormatDocument(string pguidCmdGroup, uint nCmdID)
        {
            if (pguidCmdGroup == Microsoft.VisualStudio.VSConstants.CMDSETID.StandardCommandSet97_string)
            {
                switch (nCmdID)
                {
                    case (uint)Microsoft.VisualStudio.VSConstants.VSStd97CmdID.SaveProjectItem:
                        {
                            FormatCurrentActiveDocument();
                        }
                        break;
                    case (uint)Microsoft.VisualStudio.VSConstants.VSStd97CmdID.SaveSolution:
                        {
                            FormatDocuments(GetNonSavedDocuments());
                        }
                        break;
                    default:
                        break;
                }
            }

        }

        private void FormatDocuments(IEnumerable<Document> documents)
        {
            var currentDoc = dte.ActiveDocument;
            foreach (var doc in documents)
            {
                doc.Activate();
                FormatCurrentActiveDocument();
            }
            currentDoc.Activate();
        }

        IEnumerable<Document> GetNonSavedDocuments()
        {
            return dte.Documents.OfType<Document>().Where(document => !document.Saved);
        }

    }
}