from setuptools import setup

setup(
    # Needed to silence warnings (and to be a worthwhile package)
    name='Cluster Connector NLP API',
    url='https://github.com/heckej/P-O-Entrepreneurship-Team-A-ClusterConnector',
    author='Joren Van Hecke',
    author_email='joren.vanhecke@student.kuleuven.be',
    # Needed to actually package something
    packages=['cluster'],
    # Needed for dependencies
    install_requires=['requests'],
    # *strongly* suggested for sharing
    version='0.1',
    # The license can be anything you like
    license='MIT',
    description='An example of a python package from pre-existing code',
    # We will also need a readme eventually (there will be a warning)
    long_description=open('README.txt').read(),
)