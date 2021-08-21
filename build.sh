docker build -t perjahnutils .

rm -rf artifacts
mkdir artifacts
docker run --entrypoint cp -v `pwd`/artifacts:/out perjahnutils PerJahnUtils.7z /out
mv artifacts/PerJahnUtils.7z .
