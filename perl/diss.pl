#!/usr/bin/perl
#
# here:// grandma's mixer main funcs
# Should be launched only from prefixes like diss-web.pl or diss-com.pl
# obtained text must be in @text already
$text_mixed = "";

1; #return true

#####################
# Prepare raw text  #
#####################
sub Initialize {

    srand(time); # reset the randomizer

    # Initial chunk borders
    foreach $i (0..$#text) {
        $start[$i] = int(rand() * $chunk);
        $end[$i] = $start[$i] + $base + int(rand() * $chunk);
        $end[$cur] = &ShiftSpace($text[$cur], $end[$cur]);
    }

    $cur = 0; # current textfile
    $textcount = 0;
    $samplecount = 0;
    $printsample = 0;
    $overlap = '';

}

#####################
# The mixing itself #
#####################
sub Mix {

    for ($i = 1; $i <= $nNnNn; $i++) {

        # Print out the chunk
#		print substr($text[$cur], $start[$cur], $end[$cur] - $start[$cur]);
		$text_mixed .= substr($text[$cur], $start[$cur], $end[$cur] - $start[$cur]);
        if ($printsample == 1) {
			$text_mixed .= " $sample,";
            $printsample = 0;
        }
        # Get the overlap (with $cont as its length)
        $overlap = &Overlap($text[$cur], $end[$cur]);

        # debugging
        if (defined $opt_d) {
            $text_mixed .= "\n[$overlap]\n";
        }

        # Check if ready to skip to the next textfile
        if (++$textcount == $change) {
            $textcount = 0; # reset the counter
            $cur++;
            $cur = 0 if ($cur == $#text + 1); # reset current indicator
                                              # if needed
        }

        # Find the next occurence of overlap and set $start
        $start[$cur] = index($text[$cur], $overlap, $end[$cur]) + $cont;

        # If no more occurences
        if ($start[$cur] == -1 + $cont) {
            # Then try before...
            $start[$cur] = index($text[$cur], $overlap, 0) + $cont;
        }

        # If _still_ no more occurences, go to the previous text
        if ($start[$cur] == -1 + $cont) {
            $cur--;
            $cur = $#text if ($cur < 0);
            # Then try before...
            $start[$cur] = index($text[$cur], $overlap, $end[$cur]) + $cont;
            if ($start[$cur] == -1 + $cont) {
                $start[$cur] = index($text[$cur], $overlap, 0) + $cont;
            }
        }

        if ( ($sample_enable == 1) &&
            (++$samplecount == $samplefreq) &&
            (index($text[$cur], ',', $start[$cur]) != -1) ) {
            $samplecount = 0;
            $end[$cur] = index($text[$cur], ',', $start[$cur]) + 1;
            $printsample = 1;
        }
        elsif (($sample_enable == 1) && ($samplecount == $samplefreq)) {
            $samplecount = 0;
        }
        else { # give up
            # Set new $end border
            $end[$cur] = $start[$cur] + $base + int(rand() * $chunk);
            $end[$cur] = &ShiftSpace($text[$cur], $end[$cur]);

            # Check if out of bounds
            if ($end[$cur] > length($text[$cur])) {
                #$start[$cur] = int(rand() * length($text[$cur]));
                $start[$cur] = int(rand() * $chunk);
                $end[$cur] = $start[$cur] + $base + int(rand() * $chunk);
                $end[$cur] = &ShiftSpace($text[$cur], $end[$cur]);
            }
        }
    }

}

##########################################################################
# Makes sure the overlap doesn't contain spaces (shifts it on and on...) #
##########################################################################
sub ShiftSpace {
    local ($_text, $_end) = @_;
    local ($_idx);

    while ( ($_idx = rindex(&Overlap($_text, $_end), ' ')) != ($[ - 1) ) {
        $_end += $_idx + 1; # shift the space away
    }
    return $_end;
}

sub GoComma {

}

###################
# Get the overlap #
###################
sub Overlap {
    return substr($_[0], $_[1] - $cont, $cont);
}

##############################
# Print mixed text to output #
##############################
sub PrintMixedText {

	print $mix_start;
	print $text_mixed;
	print $mix_end;
	print "\n";

}

