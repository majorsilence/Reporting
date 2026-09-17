using System;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;
using Draw = Majorsilence.Forms.Drawing;

namespace Majorsilence.Forms.Drawing
{
    public class GraphicsExtended
    {
        //drawstring justified without paragraph format
        public static void DrawStringJustified(Draw.Graphics graphics, string s, 
            Draw.Font font, Draw.Brush brush, System.Drawing.RectangleF layoutRectangle)
        {
            DrawStringJustified(graphics, s, font, brush, layoutRectangle, ' ');
        }

        //drawstring justified with paragraph format
        public static void DrawStringJustified(Draw.Graphics graphics, string s, Draw.Font font, Draw.Brush brush, 
            System.Drawing.RectangleF layoutRectangle, char paragraphFormat)
        {
            try
            {
                //save the current state of the graphics handle
                Draw.Drawing2D.GraphicsState graphicsState = graphics.Save();
                //obtain the font height to be used as line height
                double lineHeight = (double)Math.Round(font.GetHeight(graphics), 1);
                //string builder to format the text
                StringBuilder text = new StringBuilder(s);
                Draw.Font originalFont = new Draw.Font(font.FontFamily, font.Size, font.Style);

                //adjust the text string to ease detection of carriage returns
                text = text.Replace("\r\n", " <CR> ");
                text = text.Replace("\r", " <CR> ");
                text.Append(" <CR> ");

                //ensure measure string will bring the best measures possible (antialias)
                graphics.TextRenderingHint = Draw.Text.TextRenderingHint.AntiAlias;

                //create a string format object with the generic typographic to obtain the most accurate string measurements
                //strange, but the recommended for this case is to use a "cloned" stringformat
                Draw.StringFormat stringFormat = (Draw.StringFormat)Draw.StringFormat.GenericTypographic.Clone();

                //allow the correct measuring of spaces
                stringFormat.FormatFlags = Draw.StringFormatFlags.MeasureTrailingSpaces;

                //create a stringformat for leftalignment
                Draw.StringFormat leftAlignHandle = new Draw.StringFormat();
                leftAlignHandle.LineAlignment = Draw.StringAlignment.Near;

                //create a stringformat for rightalignment
                Draw.StringFormat rightAlignHandle = new Draw.StringFormat();
                rightAlignHandle.LineAlignment = Draw.StringAlignment.Far;

                //measure space for the given font
                System.Drawing.SizeF stringSize = graphics.MeasureString(" ", font, layoutRectangle.Size, stringFormat);
                double spaceWidth = stringSize.Width + 1;

                //measure paragraph format for the given font
                double paragraphFormatWidth = 0;
                if (paragraphFormat != ' ')
                {
                    System.Drawing.SizeF paragraphFormatSize = graphics.MeasureString(paragraphFormat.ToString(), 
                        new Draw.Font(font.FontFamily, font.Size, Draw.FontStyle.Regular), layoutRectangle.Size, stringFormat);
                    paragraphFormatWidth = paragraphFormatSize.Width;
                }

                //total word count
                int totalWords = Regex.Matches(text.ToString(), " ").Count;

                //array of words
                ArrayList words = new ArrayList();

                //measure each word
                int n = 0;
                while (true)
                {
                    //original word
                    string word = Regex.Split(text.ToString(), " ").GetValue(n).ToString();

                    //add to words array the word without tags
                    words.Add(new Word(word.Replace("<b>", "").Replace("</b>", "").Replace("<i>", "").Replace("</i>", "")));

                    //marque to start bolding or/and italic
                    if (word.ToLower().Contains("<b>") && word.ToLower().Contains("<i>"))
                        font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Bold & Draw.FontStyle.Italic);
                    else if (word.ToLower().Contains("<b>"))
                        font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Bold);
                    else if (word.ToLower().Contains("<i>"))
                        font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Italic);

                    Word currentWord = (Word)words[n];
                    currentWord.StartBold = word.ToLower().Contains("<b>");
                    currentWord.StopBold = word.ToLower().Contains("</b>");
                    currentWord.StartItalic = word.ToLower().Contains("<i>");
                    currentWord.StopItalic = word.ToLower().Contains("</i>");

                    //size of the word
                    System.Drawing.SizeF wordSize = graphics.MeasureString(currentWord.String, font, layoutRectangle.Size, stringFormat);
                    float wordWidth = wordSize.Width;

                    if (wordWidth > layoutRectangle.Width && currentWord.String != "<CR>")
                    {
                        int reduce = 1;
                        while (true)
                        {
                            int lengthChars = (int)Math.Round(currentWord.String.Length / (wordWidth / layoutRectangle.Width), 0) - reduce;
                            string cutWord = currentWord.String.Substring(0, lengthChars);

                            //the new size of the word
                            wordSize = graphics.MeasureString(cutWord, font, layoutRectangle.Size, stringFormat);
                            wordWidth = wordSize.Width;

                            //update the word string
                            ((Word)words[n]).String = cutWord;

                            //add new word
                            if (wordWidth <= layoutRectangle.Width)
                            {
                                totalWords++;
                                words.Add(new Word("", 0,
                                    currentWord.StartBold, currentWord.StopBold,
                                    currentWord.StartItalic, currentWord.StopItalic));
                                text.Replace(currentWord.String, cutWord + " " + currentWord.String.Substring(lengthChars + 1), 0, 1);
                                break;
                            }

                            reduce++;
                        }
                    }

                    //update the word size
                    ((Word)words[n]).Length = wordWidth;

                    //marque to stop bolding or/and italic
                    if (word.ToLower().Contains("</b>") && font.Style == Draw.FontStyle.Italic)
                        font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Italic);
                    else if (word.ToLower().Contains("</i>") && font.Style == Draw.FontStyle.Bold)
                        font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Bold);
                    else if (word.ToLower().Contains("</b>") || word.ToLower().Contains("</i>"))
                        font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Regular);

                    n++;
                    if (n > totalWords - 1)
                        break;
                }

                //before we start drawing, its wise to restore ou graphics objecto to its original state
                graphics.Restore(graphicsState);

                //restore to font to the original values
                font = new Draw.Font(originalFont.FontFamily, originalFont.Size, originalFont.Style);

                //start drawing word by word
                int currentLine = 0;
                for (int i = 0; i < totalWords; i++)
                {
                    bool endOfSentence = false;
                    double wordsWidth = 0;
                    int wordsInLine = 0;

                    int j = i;
                    for (j = i; j < totalWords; j++)
                    {
                        if (((Word)words[j]).String == "<CR>")
                        {
                            endOfSentence = true;
                            break;
                        }

                        wordsWidth += ((Word)words[j]).Length + spaceWidth;
                        if (wordsWidth > layoutRectangle.Width && j > i)
                        {
                            wordsWidth = wordsWidth - ((Word)words[j]).Length - (spaceWidth * wordsInLine);
                            break;
                        }

                        wordsInLine++;
                    }

                    if (j > totalWords)
                        endOfSentence = true;

                    double widthOfBetween = 0;
                    if (endOfSentence)
                        widthOfBetween = spaceWidth;
                    else
                        widthOfBetween = (layoutRectangle.Width - wordsWidth) / (wordsInLine - 1);

                    double currentTop = layoutRectangle.Top + (int)(currentLine * lineHeight);

                    if (currentTop > (layoutRectangle.Height + layoutRectangle.Top))
                    {
                        i = totalWords;
                        break;
                    }

                    double currentLeft = layoutRectangle.Left;

                    bool lastWord = false;
                    for (int currentWord = 0; currentWord < wordsInLine; currentWord++)
                    {
                        bool loop = false;

                        if (((Word)words[i]).String == "<CR>")
                        {
                            i++;
                            loop = true;
                        }

                        if (!loop)
                        {
                            //last word in sentence
                            if (currentWord == wordsInLine && !endOfSentence)
                                lastWord = true;

                            if (wordsInLine == 1)
                            {
                                currentLeft = layoutRectangle.Left;
                                lastWord = false;
                            }

                            System.Drawing.RectangleF rectangleF;
                            Draw.StringFormat stringFormatHandle;

                            if (lastWord)
                            {
                                rectangleF = new System.Drawing.RectangleF(layoutRectangle.Left, (float)currentTop, layoutRectangle.Width, (float)(currentTop + lineHeight));
                                stringFormatHandle = rightAlignHandle;
                            }
                            else
                            {
                                //lets zero size for word to drawstring auto-size de word
                                rectangleF = new System.Drawing.RectangleF((float)currentLeft, (float)currentTop, 0, 0);
                                stringFormatHandle = leftAlignHandle;
                            }

                            //start bolding and/or italic
                            if (((Word)words[i]).StartBold && ((Word)words[i]).StartItalic)
                                font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Bold & Draw.FontStyle.Italic);
                            else if (((Word)words[i]).StartBold)
                                font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Bold);
                            else if (((Word)words[i]).StartItalic)
                                font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Italic);

                            //draw the word
                            graphics.DrawString(((Word)words[i]).String, font, brush, rectangleF, stringFormatHandle);

                            //stop bolding and/or italic
                            if (((Word)words[i]).StopBold && font.Style == Draw.FontStyle.Italic)
                                font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Regular);
                            else if (((Word)words[i]).StopItalic && font.Style == Draw.FontStyle.Bold)
                                font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Bold);
                            else if (((Word)words[i]).StopBold || ((Word)words[i]).StopItalic)
                                font = new Draw.Font(originalFont.FontFamily, originalFont.Size, Draw.FontStyle.Regular);

                            //paragraph formating
                            if (endOfSentence && currentWord == wordsInLine - 1 && paragraphFormat != ' ')
                            {
                                currentLeft += ((Word)words[i]).Length;
                                //draw until end of line
                                while (currentLeft + paragraphFormatWidth <= layoutRectangle.Left + layoutRectangle.Width)
                                {
                                    rectangleF = new System.Drawing.RectangleF((float)currentLeft, (float)currentTop, 0, 0);
                                    //draw the paragraph format
                                    graphics.DrawString(paragraphFormat.ToString(), font, brush, rectangleF, stringFormatHandle);
                                    currentLeft += paragraphFormatWidth;
                                }
                            }
                            else
                                currentLeft += ((Word)words[i]).Length + widthOfBetween;

                            //go to next word
                            i++;
                        }
                    }

                    currentLine++;

                    if (i >= totalWords)
                        break;

                    //compensate endfor
                    if (((Word)words[i]).String != "<CR>")
                        i--;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //class to define the structure of the word
        public class Word
        {
            private string s;
            private double length;
            private bool startBold, stopBold;
            private bool startItalic, stopItalic;

            public Word(string s)
            {
                this.s = s;
                length = 0;
                this.startBold = false;
                this.stopBold = false;
                this.startItalic = false;
                this.stopItalic = false;
            }

            public Word(string s, double length, bool startBold, bool stopBold, bool startItalic, bool stopItalic)
            {
                this.s = s;
                this.length = length;
                this.startBold = startBold;
                this.stopBold = stopBold;
                this.startItalic = startItalic;
                this.stopItalic = stopItalic;
            }

            public string String
            {
                get
                {
                    return s;
                }
                set
                {
                    s = value;
                }
            }

            public double Length
            {
                get
                {
                    return length;
                }
                set
                {
                    length = value;
                }
            }

            public bool StartBold
            {
                get
                {
                    return startBold;
                }
                set
                {
                    startBold = value;
                }
            }

            public bool StopBold
            {
                get
                {
                    return stopBold;
                }
                set
                {
                    stopBold = value;
                }
            }

            public bool StartItalic
            {
                get
                {
                    return startItalic;
                }
                set
                {
                    startItalic = value;
                }
            }

            public bool StopItalic
            {
                get
                {
                    return stopItalic;
                }
                set
                {
                    stopItalic = value;
                }
            }
        }
    }
}