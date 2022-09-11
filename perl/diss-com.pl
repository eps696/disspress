#!/usr/bin/perl
#
# diss.pl invocation via command line
# 
# require 'w:\here\cgi-bin\diss.pl';
require "./diss.pl";

@text = ();

&ReadOptions(); 
&GetText(); 	
&Initialize();		# in diss.pl
&Mix();				# in diss.pl
&PrintMixedText();	# in diss.pl
exit(0);

####################
# Read all options #
####################
sub ReadOptions  {
	$base = 3;
	$chunk = 8;
	$cont = 2;				# Overlap length
	$nNnNn = 1000;			# length of generated text (in chunks)
	$change = 3;			# every $change times flip to the next textfile
	$samplefreq = 8;		# how frequent a sample is inserted
	$sample = 'и немножко еще';
	$sample_enable = 0;
	$mix_start = "";
	$mix_end = "";
}

###############################
# Get texts from command line #
###############################
sub GetText {
    for ($i = 0; $i <= $#ARGV; $i++) {
        open(DATA, "<$ARGV[$i]");
        while (<DATA>) {
             $text[$i] .= " $_";
        }
        close(DATA);
		$text[$i] =~ s/\s+/ /g;			# compress spaces
		$text[$i] =~ s/\n//g;			# no carriage returns
		$text[$i] =~ s/\cM//g;
		$text[$i] =~ s/\<[^\>]*\>//g;	# <eps> a html tagz stripping tam budet?
    }
}
