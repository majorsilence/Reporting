#! /usr/bin/env python

import asyncio
import sys
sys.path.append("..")
import os
import platform
from majorsilence_reporting_rdlcreator import majorsilence

# SETUP
def configure_paths():
    current_directory = os.path.dirname(os.path.abspath(__file__))
    base_directory = os.path.abspath(os.path.join(current_directory, '..', '..', '..'))
    db_path = os.path.abspath(os.path.join(base_directory, 'Examples', 'northwindEF.db'))

    output_directory = os.path.join(current_directory, 'output')
    if not os.path.exists(output_directory):
        os.makedirs(output_directory)

    return {
        'current_directory': current_directory,
        'base_directory': base_directory,
        'db_path': db_path,
        'output_directory': output_directory
    }

async def render_report_to_pdf():
    config = configure_paths()

    # REPORT EXAMPLE
    report = majorsilence.reporting.rdlcreator.Report()

    filepath = os.path.join(config['current_directory'], "placeholder.pdf")

    await report.create_1(filepath)

if __name__ == "__main__":
    asyncio.run(render_report_to_pdf())


