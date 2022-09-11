# https://en.wikipedia.org/wiki/Dissociated_press
import os, sys
import argparse

from progress_bar import ProgressBar

def get_args():
    parser = argparse.ArgumentParser()
    parser.add_argument('-i', '--input',    default=None)
    parser.add_argument('-o', '--output',   default='out.txt')
    parser.add_argument('-c', '--chunk',    default=8, type=int, help='Length of the continuous piece of text')
    parser.add_argument('-ovl', '--overlap', default=3, type=int, help='How many characters to overlap')
    parser.add_argument('-s', '--steps',    default=100, type=int, help='How many steps to go')
    parser.add_argument(      '--start',    default=0, type=int, help='Starting position [or line] within input text[s]')
    # operating per line
    parser.add_argument('-l', '--lines',    action='store_true', help='Respect line breaks [operate per line]')
    parser.add_argument(      '--maxlen',   default=128, type=int, help='Maximum length of the generated text line [approx]')
    return parser.parse_args()

def read_text(in_txt):
    assert os.path.isfile(in_txt), 'Cannot find file %s' % in_txt
    with open(in_txt, 'r', encoding="utf-8") as f:
        text = f.read()
    return text

def dislex_line(in_txt, chunk, overlap, steps, maxlen):
    lines_out = []
    chunks = []
    cursor = last_good_cursor = last_good_line = base_line = cur_line = step = 0
    EOF = False

    pbar = ProgressBar(len(in_txt))
    while base_line < len(in_txt):

        if EOF: # didn't find more overlaps
            if last_good_cursor > 0: # add the ending of the last overlapped line
                chunks.append(in_txt[last_good_line][last_good_cursor :])
        else:
            chunks.append(in_txt[cur_line][cursor : cursor + chunk])
        cur_out = ''.join(chunks)

        if not EOF and len(in_txt[cur_line]) >= cursor + chunk and len(cur_out) < maxlen-chunk and step < steps: # not EOF, EOL or line length limit
            step += 1
            ovl = chunks[-1][-overlap:]
            while True:
                cur_line += 1
                if cur_line == len(in_txt): 
                    EOF = True; break
                cursor = in_txt[cur_line].find(ovl)
                if cursor != -1: # found overlap
                    cursor += overlap
                    last_good_cursor = cursor + chunk
                    last_good_line = cur_line
                    break

        else: # glue together what we have found, default all counters
            if len(chunks) > 1: # drop non-mixed results
                lines_out.append(''.join(chunks))
            chunks = []
            cursor = last_good_cursor = step = 0
            EOF = False
            base_line += 1
            cur_line = base_line
            pbar.upd()

    return lines_out

def dislex_char(in_txt, chunk, overlap, steps):
    chunks = []
    cursor = base = 0

    pbar = ProgressBar(steps)
    for s in range(steps):
        chunks.append(in_txt[cursor : cursor + chunk])
        ovl = chunks[-1][-overlap:]
        base = cursor + chunk
        next = in_txt[base:].find(ovl)
        if next == -1: 
            print('Input text has ended on step', s+1)
            break
        cursor = base + next + overlap
        pbar.upd()

    if len(chunks)==steps:
        print('Successfully completed steps:', steps)
    out_txt = ''.join(chunks)
    return out_txt
    

def main():
    a = get_args()
    if a.lines is True:
        in_txt = read_text(a.input).splitlines()[a.start:]
        in_txt = [tt.strip() for tt in in_txt if tt.strip()[0] != '#']
        out_txt = dislex_line(in_txt, a.chunk, a.overlap, a.steps, a.maxlen)
        out_txt = '\n'.join(out_txt)
        # print('Result:', out_txt)
    else:
        in_txt = read_text(a.input)[a.start:]
        out_txt = dislex_char(in_txt, a.chunk, a.overlap, a.steps)
        # print('Result:', out_txt)

    with open(a.output, 'w', encoding="utf-8") as f:
        f.write(out_txt)

main()
