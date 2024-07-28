DOCKER_BUILDKIT=0 docker build -t perjahnutils .

rm -rf artifacts
mkdir artifacts
docker run --entrypoint cp -v `pwd`/artifacts:/out perjahnutils perjahnutils.7z /out
mv artifacts/perjahnutils.7z .
