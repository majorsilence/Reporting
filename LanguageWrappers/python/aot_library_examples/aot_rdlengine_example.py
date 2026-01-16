#! /usr/bin/env python

import sys
sys.path.append("..")
import os
import asyncio
from majorsilence_reporting_rdlengine import majorsilence

# SETUP
def configure_paths():
    current_directory = os.path.dirname(os.path.abspath(__file__))
    base_directory = os.path.abspath(os.path.join(current_directory, '..', '..', '..'))

    db_path = os.path.abspath(os.path.join(base_directory, 'Examples', 'northwindEF.db'))
    report_path = os.path.abspath(os.path.join(base_directory, 'Examples', 'SqliteExamples', 'SimpleTest1.rdl'))

    output_directory = os.path.join(current_directory, 'output')
    if not os.path.exists(output_directory):
        os.makedirs(output_directory)

    return {
        'current_directory': current_directory,
        'base_directory': base_directory,
        'db_path': db_path,
        'report_path': report_path,
        'output_directory': output_directory
    }


async def render_report_to_pdf():
    config = configure_paths()
    # read xml from file
    with open(config['report_path'], "r") as file:
        rdl_xml_content = file.read()

    # REPORT EXAMPLE
    rdl_parser = majorsilence.reporting.rdl.RDLParser(rdl_xml_content)
    report = await rdl_parser.parse()

    filepath = os.path.join(config['current_directory'], "placeholder.pdf")
    ofs = majorsilence.reporting.rdl.OneFileStreamGen(filepath, True)

    await report.run_get_data(None)
    await report.run_render(ofs, majorsilence.reporting.rdl.OutputPresentationType.pdf)



if __name__ == "__main__":
    asyncio.run(render_report_to_pdf())
