class HighScoresController < ApplicationController
  # GET /high_scores/top?game=solitaire&limit=5
  def top_scores
    game = params[:game]
    limit = params[:limit].to_i || 5

    if game.present? && limit > 0
      high_scores = HighScore.where(game: game).order(score: :desc).limit(limit)
      render json: high_scores, status: :ok
    else
      render json: { error: "Invalid parameters" }, status: :unprocessable_entity
    end
  end

  # POST /high_scores
  def create_score
    score = HighScore.new(high_score_params)
    if score.save
      render json: score, status: :created
    else
      render json: score.errors, status: :unprocessable_entity
    end
  end

  # PUT /high_scores/:id
  def update_score
    high_score = HighScore.find_by(id: params[:id])

    if high_score&.update(high_score_params)
      render json: high_score, status: :ok
    else
      render json: { error: "High score not found or invalid parameters" }, status: :unprocessable_entity
    end
  end

  # DELETE /high_scores/:id
  def destroy_score
    # Find the high score by its ID
    high_score = HighScore.find_by(id: params[:id])

    if high_score
      high_score.destroy
      render json: { message: "High score deleted successfully" }, status: :ok
    else
      render json: { error: "High score not found" }, status: :not_found
    end
  end

  private

  def high_score_params
    params.require(:high_score).permit(:name, :game, :score)
  end
end